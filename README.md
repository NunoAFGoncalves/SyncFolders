# SyncFolders

A command-line tool that periodically synchronizes two folders, keeping a replica as an exact copy of a source folder.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## How to Build

```bash
dotnet build
```

## How to Run

```bash
dotnet run -- <source> <replica> <interval_seconds> <log_path>
```

### Arguments

| Argument            | Description                                      |
|---------------------|--------------------------------------------------|
| `source`            | Path to the source folder                        |
| `replica`           | Path to the replica folder                       |
| `interval_seconds`  | Sync interval in seconds                         |
| `log_path`          | Path to the log file                             |

### Example

```bash
dotnet run -- /Users/me/Desktop/Origin /Users/me/Desktop/Replica 6 log.txt
```

This syncs `Origin` to `Replica` every 6 seconds, writing logs to `log.txt`.

To stop the program, press `Ctrl+C`.

## How to Run Unit Tests

```bash
dotnet test SyncFolders.Tests
```

To run a specific test or group of tests:

```bash
dotnet test SyncFolders.Tests --filter "TestMethodName"
```

## What It Does

- Copies new and modified files from source to replica
- Removes files and folders in replica that no longer exist in source
- Syncs empty directories
- Skips empty files (0 bytes)
- Validates paths are not nested to prevent infinite loops
- Checks available disk space before syncing
- Skips files with Windows-forbidden characters (`< > : " | ? *`)
- Warns about case-sensitivity conflicts between filesystems
- Handles locked files and permission errors gracefully
- Uses atomic copy (writes to `.tmp` first, then renames)
