using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PingMonitor
{
    [Serializable]
    public sealed class MonitoringListDefinition
    {
        public MonitoringListDefinition()
        {
            ServerIds = new List<string>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ServerIds { get; set; }

        public MonitoringListDefinition Clone()
        {
            return new MonitoringListDefinition
            {
                Id = Id,
                Name = Name,
                ServerIds = ServerIds == null ? new List<string>() : new List<string>(ServerIds)
            };
        }
    }

    [Serializable]
    public sealed class MonitoringListsState
    {
        public MonitoringListsState()
        {
            SchemaVersion = 1;
            ActiveListId = MonitoringListStore.AllServersListId;
            Lists = new List<MonitoringListDefinition>();
        }

        public int SchemaVersion { get; set; }
        public string ActiveListId { get; set; }
        public List<MonitoringListDefinition> Lists { get; set; }
        public string Language { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool Maximized { get; set; }
        public List<int> ColumnWidths { get; set; }
        public int SortColumn { get; set; } = -1;
        public bool SortDescending { get; set; }
        public bool ReduceMotion { get; set; }
        public bool AlertsEnabled { get; set; }
    }

    public sealed class MonitoringListStore
    {
        public const string AllServersListId = "all";

        private readonly string filePath;
        private readonly string backupPath;
        public string FilePath { get { return filePath; } }

        public MonitoringListStore()
            : this(GetDefaultFilePath())
        {
        }

        public MonitoringListStore(string customFilePath)
        {
            if (string.IsNullOrWhiteSpace(customFilePath)) throw new ArgumentException("A storage path is required.", "customFilePath");
            filePath = Path.GetFullPath(customFilePath);
            backupPath = filePath + ".bak";
        }

        public MonitoringListsState Load(IEnumerable<string> validServerIds, IDictionary<string, string> legacyIds = null)
        {
            HashSet<string> validIds = new HashSet<string>(validServerIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            MonitoringListsState state = TryLoad(filePath) ?? TryLoad(backupPath) ?? new MonitoringListsState();
            if (legacyIds != null && state.Lists != null)
                foreach (MonitoringListDefinition list in state.Lists.Where(x => x != null && x.ServerIds != null))
                    list.ServerIds = list.ServerIds.Select(id => id != null && legacyIds.ContainsKey(id) ? legacyIds[id] : id).ToList();
            Normalize(state, validIds);
            return state;
        }

        public void Save(MonitoringListsState state)
        {
            if (state == null) return;

            string directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);
            string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            XmlSerializer serializer = new XmlSerializer(typeof(MonitoringListsState));

            try
            {
                using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(stream, state);
                    stream.Flush(true);
                }
                if (File.Exists(filePath))
                    File.Replace(tempPath, filePath, TryLoad(filePath) == null ? null : backupPath, true);
                else
                    File.Move(tempPath, filePath);
            }
            finally
            {
                // Never fall back to overwriting the destination non-atomically.
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }

        private static MonitoringListsState TryLoad(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MonitoringListsState));
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return serializer.Deserialize(stream) as MonitoringListsState;
            }
            catch
            {
                return null;
            }
        }

        private static string GetDefaultFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "PingMonitor.SA", "server-lists.v1.xml");
        }

        private static void Normalize(MonitoringListsState state, HashSet<string> validServerIds)
        {
            state.SchemaVersion = 1;
            if (state.Lists == null) state.Lists = new List<MonitoringListDefinition>();

            HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<MonitoringListDefinition> normalized = new List<MonitoringListDefinition>();

            foreach (MonitoringListDefinition list in state.Lists)
            {
                if (list == null) continue;
                string name = (list.Name ?? string.Empty).Trim();
                if (name.Length == 0 || name.Length > 60 || !usedNames.Add(name)) continue;

                string id = string.IsNullOrWhiteSpace(list.Id) ? Guid.NewGuid().ToString("N") : list.Id.Trim();
                if (id.Equals(AllServersListId, StringComparison.OrdinalIgnoreCase) || !usedIds.Add(id))
                    id = Guid.NewGuid().ToString("N");

                list.Id = id;
                list.Name = name;
                list.ServerIds = (list.ServerIds ?? new List<string>())
                    .Where(validServerIds.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                normalized.Add(list);
            }

            state.Lists = normalized;
            if (string.IsNullOrWhiteSpace(state.ActiveListId) ||
                (!state.ActiveListId.Equals(AllServersListId, StringComparison.OrdinalIgnoreCase) &&
                 !state.Lists.Any(x => x.Id.Equals(state.ActiveListId, StringComparison.OrdinalIgnoreCase))))
            {
                state.ActiveListId = AllServersListId;
            }
        }
    }

    public sealed class MonitoringListComboItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public override string ToString() { return Text; }
    }
}
