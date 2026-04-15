namespace FolderSync
{
    internal static class Logger
    {
        internal static void Log(string message, string logPath)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logEntry);
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
    }
}
