namespace TESTING_WeddingtreeV1
{
    internal static class Logger
    {
        private static string loggerPath = "";

        public static void SetLoggerPath(string path)
        {
            if (path.Equals("DROPBOX"))
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData")!, infoPath);
                if (!File.Exists(jsonPath)) jsonPath = Path.Combine(Environment.GetEnvironmentVariable("AppData")!, infoPath);
                if (!File.Exists(jsonPath)) throw new Exception("Dropbox could not be found!");
                var dropboxPath = File.ReadAllText(jsonPath).Split('\"')[5].Replace(@"\\", @"\");

                path = Path.Combine(dropboxPath, @$"DHL Warenpost Programm\Logs");
            }

            loggerPath = Path.Combine(path, $"{DateTime.Now.Year}/{DateTime.Now.Month}");
        }

        public static void Log(string? body, params string[] details)
        {
            if (body is not null) Console.WriteLine(body);

            foreach (var item in details) Console.WriteLine($"{item}\n");

            if (string.IsNullOrEmpty(loggerPath)) return;

            if (!Directory.Exists(loggerPath)) Directory.CreateDirectory(loggerPath);

            string fileName = $"{DateTime.Now.Day}_{Environment.MachineName}";
#if DEBUG
            fileName += "_DEBUG";
#endif
            string file = Path.Combine(loggerPath, $"{fileName}.txt");

            if (!File.Exists(file)) File.Create(file).Close();

            using var sw = new StreamWriter(file, true);

            sw.WriteLine($"{DateTime.Now} {Environment.MachineName}");

            if (body is not null) sw.WriteLine(body);

            foreach (var item in details) sw.WriteLine(item);

            sw.WriteLine(sw.NewLine);

            Console.WriteLine($"\nSee log at: {file}");
        }
    }
}