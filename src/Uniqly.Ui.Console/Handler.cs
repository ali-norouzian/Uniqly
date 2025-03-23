using System.Diagnostics;
using System.IO.Hashing;
using Microsoft.VisualBasic.FileIO;

namespace Uniqly.Ui.Cli;
internal static class Handler
{
    //private static readonly string[] _args = [Command.FindDuplicates, "D:\\WithDups"];// E:\\Phone
    private static readonly string[] _args = [Command.ApplyChanges, "D:\\WithDups", Command.KeepNewest];// E:\\Phone
                                                                                                        //Environment.GetCommandLineArgs();
    internal static void FindDuplicates()
    {
        FindDuplicates(
            out var searchAddress,
            out var dups);

        FileInfo fileInfo = null;

        WriteResultsInFile(
        ref searchAddress,
        ref dups,
        ref fileInfo,
        out var resultFileAddress);

        OpenResultFileWithDefaultEditorAndWaitUntilClose(
            ref resultFileAddress);

        ReadResultFileAndTakeAction(
            ref resultFileAddress,
            ref fileInfo);
    }

    internal static void KeepNewestAndDeleteOthers()
    {
        var searchAddress = _args[1];
        var resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";
        FileInfo fileInfo = null;
        long deletedSize = 0;
        // It's only keep a same file paths in a time
        // FilePath, fileInfo.LastWriteTimeUtc
        var dupInfos = new Dictionary<string, DateTime>();
        using (var reader = new StreamReader(resultFileAddress))
        {
            var commands = new string[2];
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) &&
                    dupInfos.Any())
                {
                    // Remove newest from list
                    dupInfos.Remove(
                        dupInfos.OrderByDescending(e => e.Value).First().Key);

                    foreach (var (filePath, _) in dupInfos)
                    {
                        FileSystem.DeleteFile(
                            filePath,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                    }

                    // fileInfo filled with our duplicate file info
                    // And it's not null here
                    deletedSize += fileInfo.Length * dupInfos.Count;

                    // Reset for new duplicate file
                    dupInfos.Clear();

                    continue;
                }

                commands = line.Split(" ", 2);
                commands = commands.Last().Split(" | ");

                // commands.First() is file path here
                fileInfo = new FileInfo(commands.First());
                dupInfos.Add(commands.First().Trim(), fileInfo.CreationTime);
            }
        }

        Console.WriteLine($"Selected files moved to recycle bin. (Size: {FormatSize(deletedSize)})");
    }

    static void FindDuplicates(
        out string searchAddress,
        out Dictionary<ulong, List<string>> dups)
    {
        var sw = Stopwatch.StartNew();

        searchAddress = _args[1];

        var fileAddresses = Directory.GetFiles(searchAddress, "*", System.IO.SearchOption.AllDirectories);
        var fileAddressesLength = fileAddresses.Length;

        Console.WriteLine($"{fileAddressesLength} files found.");

        // 1MB buffer
        const int bufferSize = 1 * 1024 * 1024;
        Span<byte> buffer = new byte[bufferSize];
        var xxHash64 = new XxHash64();
        ulong hash;
        List<string> fileAdresses;
        dups = [];
        var index = 0;
        int bytesRead;
        foreach (var fileAddress in fileAddresses)
        {
            using (var stream = File.OpenRead(fileAddress))
            {
                while ((bytesRead = stream.Read(buffer)) > 0)
                {
                    xxHash64.Append(buffer);//.AsSpan(0, bytesRead)
                }
            }

            hash = xxHash64.GetCurrentHashAsUInt64();
            xxHash64.Reset();

            dups.TryGetValue(hash, out fileAdresses);
            if (fileAdresses is null || fileAdresses.Count == 0)
            {
                dups.Add(hash, [fileAddress]);
            }
            else
            {
                fileAdresses.Add(fileAddress);
            }

            index++;
            Console.Clear();
            Console.WriteLine(fileAddress);
            Console.WriteLine($"{index} of {fileAddressesLength}.");
        }

        Console.WriteLine(sw.Elapsed);

    }

    static void WriteResultsInFile(
        ref string searchAddress,
        ref Dictionary<ulong, List<string>> dups,
        ref FileInfo fileInfo,
        out string resultFileAddress)
    {
        // Write results in file
        resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";
        using (var writer = new StreamWriter(resultFileAddress))
        {
            foreach (var (_, values) in dups.Where(e => 1 < e.Value.Count))
            {
                foreach (var value in values)
                {
                    fileInfo = new FileInfo(value);
                    var a = $"stay/remove";
                    var b = $"| (Size: {FormatSize(fileInfo.Length)}) | (CreationTime: {fileInfo.CreationTime}, LastWriteTime: {fileInfo.LastWriteTime})";
                    var c = $"";
                    writer.WriteLine($"{a,-10} {value,-100} {b,-100}");
                }

                writer.WriteLine();
            }
        }

        Console.WriteLine(resultFileAddress);
    }

    static void OpenResultFileWithDefaultEditorAndWaitUntilClose(
        ref string resultFileAddress)
    {
        // Open the file with the default editor
        var process = Process.Start(new ProcessStartInfo(resultFileAddress) { UseShellExecute = true });
        process.WaitForExit();

    }

    static void ReadResultFileAndTakeAction(
        ref string resultFileAddress,
        ref FileInfo fileInfo)
    {
        // Read results and take action
        long deletedSize = 0;
        using (var reader = new StreamReader(resultFileAddress))
        {
            var commands = new string[2];
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                commands = line.Split(" ", 2);
                // Take action
                if (string.Equals(commands.First(), nameof(Action.Remove), StringComparison.OrdinalIgnoreCase))
                {
                    commands = commands.Last().Split(" | ");
                    fileInfo = new FileInfo(commands.First());
                    deletedSize += fileInfo.Length;

                    FileSystem.DeleteFile(commands.First(), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
        }

        Console.WriteLine($"Selected files moved to recycle bin. (Size: {FormatSize(deletedSize)})");
    }

    // Helper method to format size in a human-readable way
    static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

}
