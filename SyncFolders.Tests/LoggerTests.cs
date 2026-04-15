using FolderSync;

namespace SyncFolders.Tests;

public class LoggerTests : TestBase
{
    [Fact]
    public void Log_WritesToConsoleAndFile()
    {
        Logger.Log("test message", _logFile);

        Assert.True(File.Exists(_logFile));
        string content = File.ReadAllText(_logFile);
        Assert.Contains("test message", content);
    }

    [Fact]
    public void Log_AppendsMultipleEntries()
    {
        Logger.Log("first", _logFile);
        Logger.Log("second", _logFile);

        string[] lines = File.ReadAllLines(_logFile);
        Assert.Equal(2, lines.Length);
        Assert.Contains("first", lines[0]);
        Assert.Contains("second", lines[1]);
    }
}
