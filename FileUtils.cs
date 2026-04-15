using System.Security.Cryptography;

namespace FolderSync
{
    internal static class FileUtils
    {
        private static readonly System.Buffers.SearchValues<char> s_forbiddenChars = System.Buffers.SearchValues.Create("<>:\"|?*");

        internal static bool AreFilesEqual(string path1, string path2)
        {
            using var md5 = MD5.Create();
            using var stream1 = File.OpenRead(path1);
            using var stream2 = File.OpenRead(path2);

            byte[] hash1 = md5.ComputeHash(stream1);
            byte[] hash2 = md5.ComputeHash(stream2);

            return BitConverter.ToString(hash1) == BitConverter.ToString(hash2);
        }

        internal static bool IsEmptyFile(string path)
        {
            return new FileInfo(path).Length == 0;
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
    }
}
