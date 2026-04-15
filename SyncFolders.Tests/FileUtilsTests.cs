using System.Runtime.InteropServices;
using FolderSync;

namespace SyncFolders.Tests;

public class FileUtilsTests : TestBase
{
    [Fact]
    public void AreFilesEqual_IdenticalFiles_ReturnsTrue()
    {
        string file1 = Path.Combine(_testDir, "file1.txt");
        string file2 = Path.Combine(_testDir, "file2.txt");
        File.WriteAllText(file1, "same content");
        File.WriteAllText(file2, "same content");

        Assert.True(FileUtils.AreFilesEqual(file1, file2));
    }

    [Fact]
    public void AreFilesEqual_DifferentFiles_ReturnsFalse()
    {
        string file1 = Path.Combine(_testDir, "file1.txt");
        string file2 = Path.Combine(_testDir, "file2.txt");
        File.WriteAllText(file1, "content A");
        File.WriteAllText(file2, "content B");

        Assert.False(FileUtils.AreFilesEqual(file1, file2));
    }

    [Fact]
    public void IsEmptyFile_ReturnsTrueForEmptyFile()
    {
        string file = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(file, "");

        Assert.True(FileUtils.IsEmptyFile(file));
    }

    [Fact]
    public void IsEmptyFile_ReturnsFalseForNonEmptyFile()
    {
        string file = Path.Combine(_testDir, "notempty.txt");
        File.WriteAllText(file, "data");

        Assert.False(FileUtils.IsEmptyFile(file));
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
        Assert.True(FileUtils.HasForbiddenCharacters(name));
    }

    [Theory]
    [InlineData("normal-file.txt")]
    [InlineData("folder/file.txt")]
    [InlineData("café.txt")]
    [InlineData("日本語.txt")]
    public void HasForbiddenCharacters_WithValidName_ReturnsFalse(string name)
    {
        Assert.False(FileUtils.HasForbiddenCharacters(name));
    }

    [Fact]
    public void FindCaseSensitivityConflicts_NoConflicts_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "FileA.txt"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "FileB.txt"), "b");

        var conflicts = FileUtils.FindCaseSensitivityConflicts(_sourceDir);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void FindCaseSensitivityConflicts_WithConflicts_ReturnsPairs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        File.WriteAllText(Path.Combine(_sourceDir, "File.txt"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "file.txt"), "b");

        var conflicts = FileUtils.FindCaseSensitivityConflicts(_sourceDir);

        Assert.Single(conflicts);
    }
}
