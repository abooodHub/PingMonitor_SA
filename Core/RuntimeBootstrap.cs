using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace PingMonitor
{
    internal static class RuntimeBootstrap
    {
        private const string ReadyKey = "PingMonitor.RuntimeConfigured";

        internal static int Run(string[] args)
        {
            if (Equals(AppDomain.CurrentDomain.GetData(ReadyKey), true))
            {
                RunInterface();
                return 0;
            }

            try
            {
                string executable = typeof(RuntimeBootstrap).Assembly.Location;
                string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PingMonitor.SA", "Runtime");
                string configuration = PrepareConfiguration(dataDirectory);
                AppDomain domain = CreateApplicationDomain(executable, configuration);
                try { return domain.ExecuteAssembly(executable, args); }
                finally { AppDomain.Unload(domain); }
            }
            catch (Exception exception)
            {
                ShowStartupError(exception);
                return 1;
            }
        }

        internal static AppDomain CreateApplicationDomain(string executable, string configuration)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(executable),
                ApplicationName = Path.GetFileName(executable),
                ConfigurationFile = configuration,
                TargetFrameworkName = ".NETFramework,Version=v4.7.2"
            };
            AppDomain domain = AppDomain.CreateDomain(Path.GetFileName(executable), null, setup);
            domain.SetData(ReadyKey, true);
            return domain;
        }

        internal static string PrepareConfiguration(string directory)
        {
            byte[] expected;
            using (Stream resource = typeof(RuntimeBootstrap).Assembly.GetManifestResourceStream("PingMonitor.Runtime.config"))
            using (var buffer = new MemoryStream())
            {
                if (resource == null) throw new InvalidOperationException("The embedded runtime configuration is missing.");
                resource.CopyTo(buffer);
                expected = buffer.ToArray();
            }
            string hash;
            using (var sha = SHA256.Create()) { hash = BitConverter.ToString(sha.ComputeHash(expected)).Replace("-", "").ToLowerInvariant(); }
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, hash + ".config");
            // A content-addressed path keeps different releases from replacing each other's configuration.
            // Serialize concurrent starts, and replace a damaged file only after its replacement is complete.
            using (var mutex = new System.Threading.Mutex(false, "Local\\PingMonitor.SA.Runtime." + hash))
            {
                bool acquired = false;
                try
                {
                    try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(15)); }
                    catch (System.Threading.AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new IOException("Timed out while preparing application data. Try starting again.");
                    if (File.Exists(destination) && new FileInfo(destination).Length == expected.Length &&
                        File.ReadAllBytes(destination).SequenceEqual(expected)) return destination;
                    string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                    try
                    {
                        using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        { output.Write(expected, 0, expected.Length); output.Flush(true); }
                        if (File.Exists(destination)) File.Replace(temporary, destination, null);
                        else File.Move(temporary, destination);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                    return destination;
                }
                finally { if (acquired) mutex.ReleaseMutex(); }
            }
        }

        // Keep WinForms initialization out of the default domain: DPI configuration must be loaded first.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunInterface()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowStartupError(Exception exception)
        {
            MessageBox.Show("تعذر تشغيل البرنامج. تأكد من إمكانية الكتابة في بيانات المستخدم ثم أعد المحاولة.\n\n" +
                "Could not start PingMonitor. Check access to your local application data and try again.\n\n" + exception.Message,
                "PingMonitor.SA.v1", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
