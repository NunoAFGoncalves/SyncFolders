using System.Runtime.InteropServices;
using FolderSync;
using SyncProgram = FolderSync.Program;

namespace SyncFolders.Tests;

public class SyncFoldersTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _sourceDir;
    private readonly string _replicaDir;
    private readonly string _logFile;

    public SyncFoldersTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SyncFoldersTests_" + Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(_testDir, "source");
        _replicaDir = Path.Combine(_testDir, "replica");
        _logFile = Path.Combine(_testDir, "test.log");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_replicaDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void Log_WritesToConsoleAndFile()
    {
        SyncProgram.Log("test message", _logFile);

        Assert.True(File.Exists(_logFile));
        string content = File.ReadAllText(_logFile);
        Assert.Contains("test message", content);
    }

    [Fact]
    public void Log_AppendsMultipleEntries()
    {
        SyncProgram.Log("first", _logFile);
        SyncProgram.Log("second", _logFile);

        string[] lines = File.ReadAllLines(_logFile);
        Assert.Equal(2, lines.Length);
        Assert.Contains("first", lines[0]);
        Assert.Contains("second", lines[1]);
    }

    [Fact]
    public void AreFilesEqual_IdenticalFiles_ReturnsTrue()
    {
        string file1 = Path.Combine(_testDir, "file1.txt");
        string file2 = Path.Combine(_testDir, "file2.txt");
        File.WriteAllText(file1, "same content");
        File.WriteAllText(file2, "same content");

        Assert.True(SyncProgram.AreFilesEqual(file1, file2));
    }

    [Fact]
    public void AreFilesEqual_DifferentFiles_ReturnsFalse()
    {
        string file1 = Path.Combine(_testDir, "file1.txt");
        string file2 = Path.Combine(_testDir, "file2.txt");
        File.WriteAllText(file1, "content A");
        File.WriteAllText(file2, "content B");

        Assert.False(SyncProgram.AreFilesEqual(file1, file2));
    }

    [Fact]
    public void SyncDirectoryStructure_CreatesSubdirectoriesInReplica()
    {
        Directory.CreateDirectory(Path.Combine(_sourceDir, "sub1", "sub2"));

        SyncProgram.SyncDirectoryStructure(_sourceDir, _replicaDir, _logFile);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "sub1")));
        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "sub1", "sub2")));
    }

    [Fact]
    public void SyncDirectoryStructure_CreatesEmptyDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_sourceDir, "emptyFolder"));

        SyncProgram.SyncDirectoryStructure(_sourceDir, _replicaDir, _logFile);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "emptyFolder")));
    }

    [Fact]
    public void SyncDirectoryStructure_DoesNotRemoveExtraDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_replicaDir, "extraDir"));

        SyncProgram.SyncDirectoryStructure(_sourceDir, _replicaDir, _logFile);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "extraDir")));
    }

    [Fact]
    public void CopyOrUpdateFiles_CopiesNewFile()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "hello");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        string destFile = Path.Combine(_replicaDir, "file.txt");
        Assert.True(File.Exists(destFile));
        Assert.Equal("hello", File.ReadAllText(destFile));
    }

    [Fact]
    public void CopyOrUpdateFiles_UpdatesModifiedFile()
    {
        string srcFile = Path.Combine(_sourceDir, "file.txt");
        string destFile = Path.Combine(_replicaDir, "file.txt");
        File.WriteAllText(srcFile, "old content");
        File.WriteAllText(destFile, "old content");

        File.WriteAllText(srcFile, "new content");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        Assert.Equal("new content", File.ReadAllText(destFile));
    }

    [Fact]
    public void CopyOrUpdateFiles_SkipsIdenticalFile()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "same");
        File.WriteAllText(Path.Combine(_replicaDir, "file.txt"), "same");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        if (File.Exists(_logFile))
            Assert.DoesNotContain("COPIED", File.ReadAllText(_logFile));
    }

    [Fact]
    public void CopyOrUpdateFiles_CopiesFileInSubdirectory()
    {
        string subDir = Path.Combine(_sourceDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        Assert.True(File.Exists(Path.Combine(_replicaDir, "sub", "nested.txt")));
    }

    [Fact]
    public void RemoveOrphanFiles_DeletesFileNotInSource()
    {
        File.WriteAllText(Path.Combine(_replicaDir, "orphan.txt"), "data");

        SyncProgram.RemoveOrphanFiles(_sourceDir, _replicaDir, _logFile);

        Assert.False(File.Exists(Path.Combine(_replicaDir, "orphan.txt")));
    }

    [Fact]
    public void RemoveOrphanFiles_KeepsFileExistingInSource()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "keep.txt"), "data");
        File.WriteAllText(Path.Combine(_replicaDir, "keep.txt"), "data");

        SyncProgram.RemoveOrphanFiles(_sourceDir, _replicaDir, _logFile);

        Assert.True(File.Exists(Path.Combine(_replicaDir, "keep.txt")));
    }

    [Fact]
    public void RemoveOrphanDirectories_DeletesDirNotInSource()
    {
        Directory.CreateDirectory(Path.Combine(_replicaDir, "orphanDir"));

        SyncProgram.RemoveOrphanDirectories(_sourceDir, _replicaDir, _logFile);

        Assert.False(Directory.Exists(Path.Combine(_replicaDir, "orphanDir")));
    }

    [Fact]
    public void RemoveOrphanDirectories_KeepsDirExistingInSource()
    {
        Directory.CreateDirectory(Path.Combine(_sourceDir, "keepDir"));
        Directory.CreateDirectory(Path.Combine(_replicaDir, "keepDir"));

        SyncProgram.RemoveOrphanDirectories(_sourceDir, _replicaDir, _logFile);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "keepDir")));
    }

    [Fact]
    public void RemoveOrphanDirectories_DeletesNestedOrphanDirs()
    {
        Directory.CreateDirectory(Path.Combine(_replicaDir, "a", "b", "c"));

        SyncProgram.RemoveOrphanDirectories(_sourceDir, _replicaDir, _logFile);

        Assert.False(Directory.Exists(Path.Combine(_replicaDir, "a")));
    }

    [Fact]
    public void SyncFolders_FullSync_ReplicaMatchesSource()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "root.txt"), "root");
        Directory.CreateDirectory(Path.Combine(_sourceDir, "sub"));
        File.WriteAllText(Path.Combine(_sourceDir, "sub", "nested.txt"), "nested");
        Directory.CreateDirectory(Path.Combine(_sourceDir, "emptyDir"));

        File.WriteAllText(Path.Combine(_replicaDir, "orphan.txt"), "delete me");
        Directory.CreateDirectory(Path.Combine(_replicaDir, "orphanDir"));

        SyncProgram.SyncFolders(_sourceDir, _replicaDir, _logFile);

        Assert.True(File.Exists(Path.Combine(_replicaDir, "root.txt")));
        Assert.True(File.Exists(Path.Combine(_replicaDir, "sub", "nested.txt")));
        Assert.True(Directory.Exists(Path.Combine(_replicaDir, "emptyDir")));

        Assert.False(File.Exists(Path.Combine(_replicaDir, "orphan.txt")));
        Assert.False(Directory.Exists(Path.Combine(_replicaDir, "orphanDir")));
    }

    [Fact]
    public void SyncFolders_NonExistentSource_LogsError()
    {
        string fakeSource = Path.Combine(_testDir, "nonexistent");

        SyncProgram.SyncFolders(fakeSource, _replicaDir, _logFile);

        Assert.True(File.Exists(_logFile));
        Assert.Contains("does not exist", File.ReadAllText(_logFile));
    }

    [Fact]
    public void SyncFolders_CreatesReplicaIfMissing()
    {
        string newReplica = Path.Combine(_testDir, "newReplica");

        SyncProgram.SyncFolders(_sourceDir, newReplica, _logFile);

        Assert.True(Directory.Exists(newReplica));
    }

    [Fact]
    public void HasEnoughDiskSpace_ReturnsTrueWhenSpaceAvailable()
    {
        Assert.True(SyncProgram.HasEnoughDiskSpace(_replicaDir, 1, _logFile));
    }

    [Fact]
    public void HasEnoughDiskSpace_ReturnsFalseWhenNotEnoughSpace()
    {
        bool result = SyncProgram.HasEnoughDiskSpace(_replicaDir, long.MaxValue, _logFile);

        Assert.False(result);
        Assert.True(File.Exists(_logFile));
        Assert.Contains("Not enough disk space", File.ReadAllText(_logFile));
    }

    [Theory]
    [InlineData("file with spaces.txt")]
    [InlineData("café.txt")]
    [InlineData("日本語ファイル.txt")]
    [InlineData("émojis_🎉🚀.txt")]
    [InlineData("special (1) [2] {3}.txt")]
    [InlineData("acentos-àáâãäå.txt")]
    public void SyncFolders_CopiesFilesWithWeirdNames(string fileName)
    {
        File.WriteAllText(Path.Combine(_sourceDir, fileName), "content");

        SyncProgram.SyncFolders(_sourceDir, _replicaDir, _logFile);

        Assert.True(File.Exists(Path.Combine(_replicaDir, fileName)));
        Assert.Equal("content", File.ReadAllText(Path.Combine(_replicaDir, fileName)));
    }

    [Theory]
    [InlineData("folder with spaces")]
    [InlineData("dossier-été")]
    [InlineData("パスタ")]
    [InlineData("émojis_🎉🚀")]
    [InlineData("special (1) [2] {3}")]
    [InlineData("acentos-àáâãäå")]
    public void SyncFolders_SyncsDirectoriesWithWeirdNames(string dirName)
    {
        Directory.CreateDirectory(Path.Combine(_sourceDir, dirName));
        File.WriteAllText(Path.Combine(_sourceDir, dirName, "nested.txt"), "data");

        SyncProgram.SyncFolders(_sourceDir, _replicaDir, _logFile);

        Assert.True(Directory.Exists(Path.Combine(_replicaDir, dirName)));
        Assert.True(File.Exists(Path.Combine(_replicaDir, dirName, "nested.txt")));
    }

    [Theory]
    [InlineData("orphan café.txt")]
    [InlineData("orphan 🎉.txt")]
    public void SyncFolders_RemovesOrphanFilesWithWeirdNames(string fileName)
    {
        File.WriteAllText(Path.Combine(_replicaDir, fileName), "data");

        SyncProgram.SyncFolders(_sourceDir, _replicaDir, _logFile);

        Assert.False(File.Exists(Path.Combine(_replicaDir, fileName)));
    }

    [Theory]
    [InlineData("orphan dossier été")]
    [InlineData("orphan 🚀")]
    public void SyncFolders_RemovesOrphanDirectoriesWithWeirdNames(string dirName)
    {
        Directory.CreateDirectory(Path.Combine(_replicaDir, dirName));

        SyncProgram.SyncFolders(_sourceDir, _replicaDir, _logFile);

        Assert.False(Directory.Exists(Path.Combine(_replicaDir, dirName)));
    }

    [Fact]
    public void CopyOrUpdateFiles_SkipsLockedSourceFile()
    {
        string srcFile = Path.Combine(_sourceDir, "locked.txt");
        File.WriteAllText(srcFile, "data");

        using var stream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.None);
        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        Assert.False(File.Exists(Path.Combine(_replicaDir, "locked.txt")));
        Assert.Contains("SKIPPED", File.ReadAllText(_logFile));
    }

    [Fact]
    public void RemoveOrphanFiles_SkipsWhenDirectoryNotWritable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        string subDir = Path.Combine(_replicaDir, "protected");
        Directory.CreateDirectory(subDir);
        string repFile = Path.Combine(subDir, "orphan.txt");
        File.WriteAllText(repFile, "data");

        File.SetUnixFileMode(subDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        SyncProgram.RemoveOrphanFiles(_sourceDir, _replicaDir, _logFile);

        File.SetUnixFileMode(subDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Assert.True(File.Exists(repFile));
        Assert.Contains("SKIPPED", File.ReadAllText(_logFile));
    }

    [Fact]
    public void CopyOrUpdateFiles_SkipsWhenDestinationNotWritable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        string subDir = Path.Combine(_sourceDir, "protected");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file.txt"), "new content");

        string destSubDir = Path.Combine(_replicaDir, "protected");
        Directory.CreateDirectory(destSubDir);
        File.WriteAllText(Path.Combine(destSubDir, "file.txt"), "old content");
        File.SetUnixFileMode(destSubDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        File.SetUnixFileMode(destSubDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Assert.Contains("SKIPPED", File.ReadAllText(_logFile));
    }

    [Fact]
    public void RemoveOrphanFiles_SkipsReadOnlyFile()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        string subDir = Path.Combine(_replicaDir, "protected");
        Directory.CreateDirectory(subDir);
        string repFile = Path.Combine(subDir, "orphan.txt");
        File.WriteAllText(repFile, "data");
        File.SetUnixFileMode(subDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        SyncProgram.RemoveOrphanFiles(_sourceDir, _replicaDir, _logFile);

        File.SetUnixFileMode(subDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Assert.True(File.Exists(repFile));
        Assert.Contains("SKIPPED", File.ReadAllText(_logFile));
    }

    [Fact]
    public void IsEmptyFile_ReturnsTrueForEmptyFile()
    {
        string file = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(file, "");

        Assert.True(SyncProgram.IsEmptyFile(file));
    }

    [Fact]
    public void IsEmptyFile_ReturnsFalseForNonEmptyFile()
    {
        string file = Path.Combine(_testDir, "notempty.txt");
        File.WriteAllText(file, "data");

        Assert.False(SyncProgram.IsEmptyFile(file));
    }

    [Fact]
    public void CopyOrUpdateFiles_SkipsEmptyFiles()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "empty.txt"), "");
        File.WriteAllText(Path.Combine(_sourceDir, "notempty.txt"), "data");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        Assert.False(File.Exists(Path.Combine(_replicaDir, "empty.txt")));
        Assert.True(File.Exists(Path.Combine(_replicaDir, "notempty.txt")));
    }

    [Fact]
    public void ArePathsNested_ReplicaInsideSource_ReturnsTrue()
    {
        string source = Path.Combine(_testDir, "parent");
        string replica = Path.Combine(_testDir, "parent", "child");

        Assert.True(SyncProgram.ArePathsNested(source, replica));
    }

    [Fact]
    public void ArePathsNested_SourceInsideReplica_ReturnsTrue()
    {
        string source = Path.Combine(_testDir, "parent", "child");
        string replica = Path.Combine(_testDir, "parent");

        Assert.True(SyncProgram.ArePathsNested(source, replica));
    }

    [Fact]
    public void ArePathsNested_SamePath_ReturnsTrue()
    {
        Assert.True(SyncProgram.ArePathsNested(_sourceDir, _sourceDir));
    }

    [Fact]
    public void ArePathsNested_IndependentPaths_ReturnsFalse()
    {
        Assert.False(SyncProgram.ArePathsNested(_sourceDir, _replicaDir));
    }

    [Fact]
    public void SyncFolders_NestedPaths_LogsErrorAndAborts()
    {
        string nested = Path.Combine(_sourceDir, "replica");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "data");

        SyncProgram.SyncFolders(_sourceDir, nested, _logFile);

        Assert.Contains("must not be nested", File.ReadAllText(_logFile));
        Assert.False(File.Exists(Path.Combine(nested, "file.txt")));
    }

    [Fact]
    public void ValidatePaths_ValidPaths_ReturnsTrue()
    {
        Assert.True(SyncProgram.ValidatePaths(_sourceDir, _replicaDir, _logFile));
    }

    [Fact]
    public void ValidatePaths_NonExistentSource_ReturnsFalse()
    {
        string fakeSource = Path.Combine(_testDir, "nonexistent");

        Assert.False(SyncProgram.ValidatePaths(fakeSource, _replicaDir, _logFile));
        Assert.Contains("does not exist", File.ReadAllText(_logFile));
    }

    [Fact]
    public void ValidatePaths_NestedPaths_ReturnsFalse()
    {
        string nested = Path.Combine(_sourceDir, "child");
        Directory.CreateDirectory(nested);

        Assert.False(SyncProgram.ValidatePaths(_sourceDir, nested, _logFile));
        Assert.Contains("must not be nested", File.ReadAllText(_logFile));
    }

    [Theory]
    [InlineData("file<name.txt")]
    [InlineData("file>name.txt")]
    [InlineData("file:name.txt")]
    [InlineData("file\"name.txt")]
    [InlineData("file|name.txt")]
    [InlineData("file?name.txt")]
    [InlineData("file*name.txt")]
    public void HasForbiddenCharacters_WithForbiddenChar_ReturnsTrue(string name)
    {
        Assert.True(SyncProgram.HasForbiddenCharacters(name));
    }

    [Theory]
    [InlineData("normal-file.txt")]
    [InlineData("folder/file.txt")]
    [InlineData("café.txt")]
    [InlineData("日本語.txt")]
    public void HasForbiddenCharacters_WithValidName_ReturnsFalse(string name)
    {
        Assert.False(SyncProgram.HasForbiddenCharacters(name));
    }

    [Fact]
    public void CopyOrUpdateFiles_SkipsFilesWithForbiddenChars()
    {
        string forbiddenFile = Path.Combine(_sourceDir, "file:name.txt");
        string normalFile = Path.Combine(_sourceDir, "normal.txt");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        File.WriteAllText(forbiddenFile, "data");
        File.WriteAllText(normalFile, "data");

        SyncProgram.CopyOrUpdateFiles(_sourceDir, _replicaDir, _logFile);

        Assert.False(File.Exists(Path.Combine(_replicaDir, "file:name.txt")));
        Assert.True(File.Exists(Path.Combine(_replicaDir, "normal.txt")));
        Assert.Contains("forbidden chars", File.ReadAllText(_logFile));
    }

    [Fact]
    public void FindCaseSensitivityConflicts_NoConflicts_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "FileA.txt"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "FileB.txt"), "b");

        var conflicts = SyncProgram.FindCaseSensitivityConflicts(_sourceDir);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void FindCaseSensitivityConflicts_WithConflicts_ReturnsPairs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        File.WriteAllText(Path.Combine(_sourceDir, "File.txt"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "b");

        var conflicts = SyncProgram.FindCaseSensitivityConflicts(_sourceDir);

        Assert.Single(conflicts);
    }
}
