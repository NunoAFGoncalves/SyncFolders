namespace SyncFolders.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly string _testDir;
    protected readonly string _sourceDir;
    protected readonly string _replicaDir;
    protected readonly string _logFile;

    protected TestBase()
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
}
