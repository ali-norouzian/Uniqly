using System.Diagnostics;
using System.IO.Hashing;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;

namespace Uniqly.Ui.Cli;
internal static class Handler
{
    //private static readonly string[] _args = [Command.FindDuplicates, "D:\\WithDups"];// E:\\Phone
    //private static readonly string[] _args = [Command.ApplyChanges, "D:\\WithDups", Command.KeepNewest];// E:\\Phone
    private static readonly string[] _args = Environment.GetCommandLineArgs();
    private const string Spaces = "                         ";

    internal static void FindDuplicates(int indexOfSearchAddressArg)
    {
        FindDuplicates(
            indexOfSearchAddressArg,
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

    internal static void DoTheDefaultApplyBehavior(int indexOfSearchAddressArg)
    {
        var resultFileAddress = GetResultFileAddress(indexOfSearchAddressArg);

        FileInfo fileInfo = null;

        ReadResultFileAndTakeAction(
                    ref resultFileAddress,
                    ref fileInfo);
    }

    internal static void KeepNewestAndDeleteOthers(int indexOfSearchAddressArg)
    {
        var searchAddress = _args[indexOfSearchAddressArg];
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
                    dupInfos.Count != 0)
                {
                    // Remove newest from list
                    dupInfos.Remove(
                        dupInfos.OrderByDescending(e => e.Value).First().Key);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (var (filePath, _) in dupInfos)
                    {
                        try
                        {
                            FileSystem.DeleteFile(
                                filePath,
                                UIOption.OnlyErrorDialogs,
                                RecycleOption.SendToRecycleBin);
                        }
                        catch (FileNotFoundException)
                        {
                            Console.WriteLine($"File in path '{filePath}' not found.");
                        }
                    }
                    Console.ResetColor();

                    // fileInfo filled with our duplicate file info
                    // And it's not null here
                    try
                    {
                        deletedSize += fileInfo.Length * dupInfos.Count;
                    }
                    catch (FileNotFoundException)
                    {
                    }

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

        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"Selected files moved to recycle bin. (Size: {FormatSize(deletedSize)})");
        Console.ResetColor();
    }

    internal static void Version()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;

        Console.WriteLine($"v{version.Major}.{version.Minor}.{version.Build}");
    }

    internal static void Help()
    {
        var helpText = $@"usage: uniqly [command] [arg] [flags]";

        Console.WriteLine(helpText);
        Console.WriteLine();

        Console.WriteLine($"{"Command",-10} {"Arg",-15} {"Flags(Optional)",-25} {"Description",-10}");
        Console.WriteLine(new string('-', 110));

        helpText = @$"{"",-10} {"",-15} {$"[{Command.H} | {Command.Help}]",-25} {"Helping document",-10}";
        Console.WriteLine(helpText);

        helpText = @$"{"",-10} {"",-15} {$"[{Command.V} | {Command.Version}]",-25} {"Show version of uniqly",-10}";
        Console.WriteLine(helpText);

        helpText = @$"{Command.Search,-10} {"<file-path>",-15} {"",-25} {"Search in path and it's sub paths for duplicate files",-10}";
        Console.WriteLine(helpText);

        helpText = @$"{Command.Apply,-10} {"<file-path>",-15} {$"[{Command.KN} | {Command.KeepNewest}]",-25} {"Apply changes that you selected on result file",-10}";
        Console.WriteLine(helpText);
    }

    static string GetSearchAddress(int indexOfSearchAddressArg)
    {
        var searchAddress = _args[indexOfSearchAddressArg];

        return searchAddress;
    }

    static string GetResultFileAddress(string searchAddress)
    {
        var resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";

        return resultFileAddress;
    }

    static string GetResultFileAddress(int indexOfSearchAddressArg)
    {
        var searchAddress = GetSearchAddress(indexOfSearchAddressArg);
        var resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";

        return resultFileAddress;
    }

    static void FindDuplicates(
        int indexOfSearchAddressArg,
        out string searchAddress,
        out Dictionary<ulong, List<string>> dups)
    {
        var sw = Stopwatch.StartNew();

        searchAddress = _args[indexOfSearchAddressArg];

        var fileAddresses = Directory.GetFiles(searchAddress, "*", System.IO.SearchOption.AllDirectories);
        var fileAddressesLength = fileAddresses.Length;

        Console.ForegroundColor = ConsoleColor.Green;
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
            //Console.Clear();
            //Thread.Sleep(1000);
            Console.Write($"\r[{sw.Elapsed}] {index} of {fileAddressesLength}: {fileAddress}{Spaces}");
        }

        //Console.Clear();
        Console.Write($"\r{index} file checked.{Spaces}{Spaces}{Spaces}{Spaces}{Spaces}{Spaces}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Time taken: {sw.Elapsed}");

    }

    static void WriteResultsInFile(
        ref string searchAddress,
        ref Dictionary<ulong, List<string>> dups,
        ref FileInfo fileInfo,
        out string resultFileAddress)
    {
        dups = dups.Where(e => 1 < e.Value.Count).ToDictionary();
        if (dups.Count == 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"No duplicate file found on '{searchAddress}' and sub paths.");
            Console.ResetColor();
            Environment.Exit(0);
        }

        // Write results in file
        resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";
        string section1 = "stay/remove", section2;
        using (var writer = new StreamWriter(resultFileAddress))
        {
            foreach (var (_, values) in dups)
            {
                foreach (var value in values)
                {
                    fileInfo = new FileInfo(value);

                    section2 = $"| (Size: {FormatSize(fileInfo.Length)}) | (CreationTime: {fileInfo.CreationTime}, LastWriteTime: {fileInfo.LastWriteTime})";

                    writer.WriteLine($"{section1,-10} {value,-100} {section2,-100}");
                }

                writer.WriteLine();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"For appling changes look at: {resultFileAddress}");
    }

    static void OpenResultFileWithDefaultEditorAndWaitUntilClose(
        ref string resultFileAddress)
    {
        // Open the file with the default editor
        using var process = Process.Start(
            new ProcessStartInfo(resultFileAddress) { UseShellExecute = true });
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
                    try
                    {
                        deletedSize += fileInfo.Length;
                    }
                    catch (FileNotFoundException)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"File in path '{commands.First().Trim()}' not found.");
                        Console.ResetColor();

                        continue;
                    }

                    FileSystem.DeleteFile(commands.First(), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"Selected files moved to recycle bin. (Size: {FormatSize(deletedSize)})");
        Console.ResetColor();
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
