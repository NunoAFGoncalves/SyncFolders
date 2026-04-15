namespace FolderSync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: FolderSync.exe <source> <replica> <interval_seconds> <log_path>");
                return;
            }

            string sourcePath = args[0];
            string replicaPath = args[1];
            int interval = int.Parse(args[2]) * 1000;
            string logFilePath = args[3];

            Console.WriteLine($"Sync started: {sourcePath} -> {replicaPath}");
            Console.WriteLine($"Interval: {args[2]}s | Log: {logFilePath}");

            while (true)
            {
                try
                {
                    SyncFolders(sourcePath, replicaPath, logFilePath);
                }
                catch (Exception ex)
                {
                    Logger.Log($"CRITICAL ERROR: {ex.Message}", logFilePath);
                }

                Thread.Sleep(interval);
            }
        }

        internal static void SyncFolders(string source, string replica, string logFile)
        {
            if (!PathValidator.ValidatePaths(source, replica, logFile))
                return;

            if (!Directory.Exists(replica))
            {
                Directory.CreateDirectory(replica);
                Logger.Log($"Replica directory created: {replica}", logFile);
            }

            long sourceSize = FileUtils.GetDirectorySize(source);
            if (!PathValidator.HasEnoughDiskSpace(replica, sourceSize, logFile))
                return;

            SyncDirectoryStructure(source, replica, logFile);

            var conflicts = FileUtils.FindCaseSensitivityConflicts(source);
            foreach (var conflict in conflicts)
            {
                Logger.Log($"WARNING (case conflict): {conflict}", logFile);
            }

            CopyOrUpdateFiles(source, replica, logFile);
            RemoveOrphanFiles(source, replica, logFile);
            RemoveOrphanDirectories(source, replica, logFile);
        }

        internal static void SyncDirectoryStructure(string source, string replica, string logFile)
        {
            foreach (string srcDir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, srcDir);
                string destDir = Path.Combine(replica, relativePath);

                if (FileUtils.HasForbiddenCharacters(relativePath))
                {
                    Logger.Log($"SKIPPED (forbidden chars): {relativePath}", logFile);
                    continue;
                }

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    Logger.Log($"CREATED (Folder): {relativePath}", logFile);
                }
            }
        }

        internal static void CopyOrUpdateFiles(string source, string replica, string logFile)
        {
            foreach (string srcFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, srcFile);
                string destFile = Path.Combine(replica, relativePath);

                if (FileUtils.IsEmptyFile(srcFile))
                    continue;

                if (FileUtils.HasForbiddenCharacters(relativePath))
                {
                    Logger.Log($"SKIPPED (forbidden chars): {relativePath}", logFile);
                    continue;
                }

                try
                {
                    string? destDir = Path.GetDirectoryName(destFile);
                    if (destDir != null && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    if (!File.Exists(destFile) || !FileUtils.AreFilesEqual(srcFile, destFile))
                    {
                        string tmpFile = destFile + ".tmp";
                        try
                        {
                            File.Copy(srcFile, tmpFile, true);
                            File.Move(tmpFile, destFile, true);
                            Logger.Log($"COPIED: {relativePath}", logFile);
                        }
                        catch
                        {
                            if (File.Exists(tmpFile))
                                try { File.Delete(tmpFile); } catch { }
                            throw;
                        }
                    }
                }
                catch (IOException ex)
                {
                    Logger.Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
                }
            }
        }

        internal static void RemoveOrphanFiles(string source, string replica, string logFile)
        {
            foreach (string repFile in Directory.GetFiles(replica, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(replica, repFile);
                string srcFile = Path.Combine(source, relativePath);

                if (!File.Exists(srcFile))
                {
                    try
                    {
                        File.Delete(repFile);
                        Logger.Log($"REMOVED (File): {relativePath}", logFile);
                    }
                    catch (IOException ex)
                    {
                        Logger.Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Logger.Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
                    }
                }
            }
        }

        internal static void RemoveOrphanDirectories(string source, string replica, string logFile)
        {
            foreach (string repDir in Directory.GetDirectories(replica, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                string relativePath = Path.GetRelativePath(replica, repDir);
                string srcDir = Path.Combine(source, relativePath);

                if (!Directory.Exists(srcDir))
                {
                    try
                    {
                        Directory.Delete(repDir, true);
                        Logger.Log($"REMOVED (Folder): {relativePath}", logFile);
                    }
                    catch (IOException ex)
                    {
                        Logger.Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Logger.Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
                    }
                }
            }
        }
    }
}