using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PingMonitor
{
    public sealed class ListImportPreview
    {
        public List<MonitoringListDefinition> Lists { get; set; }
        public int UnknownServers { get; set; }
        public int RenamedLists { get; set; }
    }

    public static class MonitoringListTransfer
    {
        public const int MaximumBytes = 1024 * 1024;
        public static ListImportPreview Read(string path, IEnumerable<ServerDefinition> catalog, IEnumerable<MonitoringListDefinition> existing)
        {
            XDocument document;
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Length > MaximumBytes) throw new InvalidDataException("List file exceeds 1 MB.");
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumBytes };
                using (XmlReader reader = XmlReader.Create(stream, settings)) document = XDocument.Load(reader);
            }
            XElement root = document.Root;
            if (root == null || root.Name != "PingMonitorLists" || (string)root.Attribute("version") != "1")
                throw new InvalidDataException("Unsupported list file format or version.");
            XElement[] elements = root.Elements().ToArray();
            if (elements.Length == 0 || elements.Length > 100 || elements.Any(x => x.Name != "List"))
                throw new InvalidDataException("The file must contain between 1 and 100 lists.");
            var known = new HashSet<string>(catalog.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var aliases = ServerCatalog.GetLegacyIdMap();
            var names = new HashSet<string>(existing.Select(x => x.Name), StringComparer.OrdinalIgnoreCase) { "All servers", "كل الخوادم" };
            var preview = new ListImportPreview { Lists = new List<MonitoringListDefinition>() };
            foreach (XElement element in elements)
            {
                string name = ((string)element.Attribute("name") ?? "").Trim();
                if (name.Length == 0 || name.Length > 60 || name.Any(char.IsControl)) throw new InvalidDataException("Invalid list name (1-60 characters required).");
                XElement[] ids = element.Elements().ToArray();
                if (ids.Length > 1000 || ids.Any(x => x.Name != "Server" || x.HasElements)) throw new InvalidDataException("Invalid server list.");
                string uniqueName = name;
                int suffix = 2;
                while (names.Contains(uniqueName))
                {
                    string end = " (" + suffix++ + ")";
                    uniqueName = name.Substring(0, Math.Min(name.Length, 60 - end.Length)) + end;
                }
                if (uniqueName != name) preview.RenamedLists++;
                names.Add(uniqueName);
                var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement idElement in ids)
                {
                    string id = idElement.Value.Trim();
                    if (aliases.ContainsKey(id)) id = aliases[id];
                    if (known.Contains(id)) selected.Add(id); else preview.UnknownServers++;
                }
                preview.Lists.Add(new MonitoringListDefinition { Id = Guid.NewGuid().ToString("N"), Name = uniqueName, ServerIds = selected.ToList() });
            }
            return preview;
        }

        public static void Export(string path, IEnumerable<MonitoringListDefinition> lists)
        {
            var source = lists.ToArray();
            if (source.Length == 0 || source.Length > 100) throw new InvalidDataException("Export between 1 and 100 lists at a time.");
            var document = new XDocument(new XElement("PingMonitorLists", new XAttribute("version", "1"), source.Select(list =>
                new XElement("List", new XAttribute("name", list.Name), list.ServerIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(id => new XElement("Server", id))))));
            string destination = Path.GetFullPath(path);
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { document.Save(stream); stream.Flush(true); }
                if (new FileInfo(temporary).Length > MaximumBytes) throw new InvalidDataException("Export exceeds 1 MB.");
                if (File.Exists(destination)) File.Replace(temporary, destination, null); else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
