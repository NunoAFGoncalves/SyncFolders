using FolderSync;

namespace SyncFolders.Tests;

public class PathValidatorTests : TestBase
{
    [Fact]
    public void ValidatePaths_ValidPaths_ReturnsTrue()
    {
        Assert.True(PathValidator.ValidatePaths(_sourceDir, _replicaDir, _logFile));
    }

    [Fact]
    public void ValidatePaths_NonExistentSource_ReturnsFalse()
    {
        string fakeSource = Path.Combine(_testDir, "nonexistent");

        Assert.False(PathValidator.ValidatePaths(fakeSource, _replicaDir, _logFile));
        Assert.Contains("does not exist", File.ReadAllText(_logFile));
    }

    [Fact]
    public void ValidatePaths_NestedPaths_ReturnsFalse()
    {
        string nested = Path.Combine(_sourceDir, "child");
        Directory.CreateDirectory(nested);

        Assert.False(PathValidator.ValidatePaths(_sourceDir, nested, _logFile));
        Assert.Contains("must not be nested", File.ReadAllText(_logFile));
    }

    [Fact]
    public void ArePathsNested_ReplicaInsideSource_ReturnsTrue()
    {
        string source = Path.Combine(_testDir, "parent");
        string replica = Path.Combine(_testDir, "parent", "child");

        Assert.True(PathValidator.ArePathsNested(source, replica));
    }

    [Fact]
    public void ArePathsNested_SourceInsideReplica_ReturnsTrue()
    {
        string source = Path.Combine(_testDir, "parent", "child");
        string replica = Path.Combine(_testDir, "parent");

        Assert.True(PathValidator.ArePathsNested(source, replica));
    }

    [Fact]
    public void ArePathsNested_SamePath_ReturnsTrue()
    {
        Assert.True(PathValidator.ArePathsNested(_sourceDir, _sourceDir));
    }

    [Fact]
    public void ArePathsNested_IndependentPaths_ReturnsFalse()
    {
        Assert.False(PathValidator.ArePathsNested(_sourceDir, _replicaDir));
    }

    [Fact]
    public void HasEnoughDiskSpace_ReturnsTrueWhenSpaceAvailable()
    {
        Assert.True(PathValidator.HasEnoughDiskSpace(_replicaDir, 1, _logFile));
    }

    [Fact]
    public void HasEnoughDiskSpace_ReturnsFalseWhenNotEnoughSpace()
    {
        bool result = PathValidator.HasEnoughDiskSpace(_replicaDir, long.MaxValue, _logFile);

        Assert.False(result);
        Assert.True(File.Exists(_logFile));
        Assert.Contains("Not enough disk space", File.ReadAllText(_logFile));
    }
}
