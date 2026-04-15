namespace FolderSync
{
    internal static class PathValidator
    {
        internal static bool ValidatePaths(string source, string replica, string logFile)
        {
            if (!Directory.Exists(source))
            {
                Logger.Log($"Error: Source folder does not exist: {source}", logFile);
                return false;
            }

            if (ArePathsNested(source, replica))
            {
                Logger.Log($"Error: Source and replica paths must not be nested. Source: {source}, Replica: {replica}", logFile);
                return false;
            }

            return true;
        }

        internal static bool ArePathsNested(string source, string replica)
        {
            string fullSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullReplica = Path.GetFullPath(replica).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return fullSource.StartsWith(fullReplica, StringComparison.OrdinalIgnoreCase)
                || fullReplica.StartsWith(fullSource, StringComparison.OrdinalIgnoreCase)
                || fullSource.TrimEnd(Path.DirectorySeparatorChar).Equals(fullReplica.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        internal static bool HasEnoughDiskSpace(string replicaPath, long requiredBytes, string logFile)
        {
            DriveInfo drive = new(Path.GetPathRoot(Path.GetFullPath(replicaPath))!);
            if (drive.AvailableFreeSpace < requiredBytes)
            {
                Logger.Log($"Error: Not enough disk space. Required: {requiredBytes} bytes, Available: {drive.AvailableFreeSpace} bytes", logFile);
                return false;
            }
            return true;
        }
    }
}
