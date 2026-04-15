using System.Security.Cryptography;

namespace FolderSync
{
    public class Program
    {
        private static readonly System.Buffers.SearchValues<char> s_forbiddenChars = System.Buffers.SearchValues.Create("<>:\"|?*");

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
                    Log($"CRITICAL ERROR: {ex.Message}", logFilePath);
                }

                Thread.Sleep(interval);
            }
        }

        internal static void SyncFolders(string source, string replica, string logFile)
        {
            if (!ValidatePaths(source, replica, logFile))
                return;

            if (!Directory.Exists(replica))
            {
                Directory.CreateDirectory(replica);
                Log($"Replica directory created: {replica}", logFile);
            }

            long sourceSize = GetDirectorySize(source);
            if (!HasEnoughDiskSpace(replica, sourceSize, logFile))
                return;

            SyncDirectoryStructure(source, replica, logFile);

            var conflicts = FindCaseSensitivityConflicts(source);
            foreach (var conflict in conflicts)
            {
                Log($"WARNING (case conflict): {conflict}", logFile);
            }

            CopyOrUpdateFiles(source, replica, logFile);
            RemoveOrphanFiles(source, replica, logFile);
            RemoveOrphanDirectories(source, replica, logFile);
        }

        internal static bool ValidatePaths(string source, string replica, string logFile)
        {
            if (!Directory.Exists(source))
            {
                Log($"Error: Source folder does not exist: {source}", logFile);
                return false;
            }

            if (ArePathsNested(source, replica))
            {
                Log($"Error: Source and replica paths must not be nested. Source: {source}, Replica: {replica}", logFile);
                return false;
            }

            return true;
        }

        internal static bool HasEnoughDiskSpace(string replicaPath, long requiredBytes, string logFile)
        {
            DriveInfo drive = new(Path.GetPathRoot(Path.GetFullPath(replicaPath))!);
            if (drive.AvailableFreeSpace < requiredBytes)
            {
                Log($"Error: Not enough disk space. Required: {requiredBytes} bytes, Available: {drive.AvailableFreeSpace} bytes", logFile);
                return false;
            }
            return true;
        }

        internal static void SyncDirectoryStructure(string source, string replica, string logFile)
        {
            foreach (string srcDir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, srcDir);
                string destDir = Path.Combine(replica, relativePath);

                if (HasForbiddenCharacters(relativePath))
                {
                    Log($"SKIPPED (forbidden chars): {relativePath}", logFile);
                    continue;
                }

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    Log($"CREATED (Folder): {relativePath}", logFile);
                }
            }
        }

        internal static void CopyOrUpdateFiles(string source, string replica, string logFile)
        {
            foreach (string srcFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, srcFile);
                string destFile = Path.Combine(replica, relativePath);

                if (IsEmptyFile(srcFile))
                    continue;

                if (HasForbiddenCharacters(relativePath))
                {
                    Log($"SKIPPED (forbidden chars): {relativePath}", logFile);
                    continue;
                }

                try
                {
                    string? destDir = Path.GetDirectoryName(destFile);
                    if (destDir != null && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    if (!File.Exists(destFile) || !AreFilesEqual(srcFile, destFile))
                    {
                        string tmpFile = destFile + ".tmp";
                        try
                        {
                            File.Copy(srcFile, tmpFile, true);
                            File.Move(tmpFile, destFile, true);
                            Log($"COPIED: {relativePath}", logFile);
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
                    Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
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
                        Log($"REMOVED (File): {relativePath}", logFile);
                    }
                    catch (IOException ex)
                    {
                        Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
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
                        Log($"REMOVED (Folder): {relativePath}", logFile);
                    }
                    catch (IOException ex)
                    {
                        Log($"SKIPPED (locked/IO): {relativePath} - {ex.Message}", logFile);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"SKIPPED (permission): {relativePath} - {ex.Message}", logFile);
                    }
                }
            }
        }

        internal static bool AreFilesEqual(string path1, string path2)
        {
            using var md5 = MD5.Create();
            using var stream1 = File.OpenRead(path1);
            using var stream2 = File.OpenRead(path2);

            byte[] hash1 = md5.ComputeHash(stream1);
            byte[] hash2 = md5.ComputeHash(stream2);

            return BitConverter.ToString(hash1) == BitConverter.ToString(hash2);
        }

        internal static bool ArePathsNested(string source, string replica)
        {
            string fullSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullReplica = Path.GetFullPath(replica).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return fullSource.StartsWith(fullReplica, StringComparison.OrdinalIgnoreCase)
                || fullReplica.StartsWith(fullSource, StringComparison.OrdinalIgnoreCase)
                || fullSource.TrimEnd(Path.DirectorySeparatorChar).Equals(fullReplica.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        internal static long GetDirectorySize(string path)
        {
            long size = 0;
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }
            return size;
        }

        internal static bool IsEmptyFile(string path)
        {
            return new FileInfo(path).Length == 0;
        }

        internal static bool HasForbiddenCharacters(string relativePath)
        {
            return relativePath.AsSpan().IndexOfAny(s_forbiddenChars) >= 0;
        }

        internal static List<string> FindCaseSensitivityConflicts(string directoryPath)
        {
            var conflicts = new List<string>();
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(directoryPath, file);
                if (seen.TryGetValue(relative, out string? existing))
                {
                    conflicts.Add($"{existing} <-> {relative}");
                }
                else
                {
                    seen[relative] = relative;
                }
            }

            foreach (string dir in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(directoryPath, dir);
                if (seen.TryGetValue(relative, out string? existing))
                {
                    conflicts.Add($"{existing} <-> {relative}");
                }
                else
                {
                    seen[relative] = relative;
                }
            }

            return conflicts;
        }

        internal static void Log(string message, string logPath)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logEntry);
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
    }
}