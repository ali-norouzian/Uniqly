using System.Diagnostics;
using System.IO.Hashing;
using Microsoft.VisualBasic.FileIO;

namespace Uniqly.Ui.Cli;
internal static class Handler
{
    private static readonly string[] _args = [Const.FindDuplicates, "D:\\WithDups"];// E:\\Phone
                                                                                    //Environment.GetCommandLineArgs();

    internal static void FindDuplicates()
    {
        var sw = Stopwatch.StartNew();

        var searchAddress = _args[1];

        var fileAddresses = Directory.GetFiles(searchAddress, "*", System.IO.SearchOption.AllDirectories);
        var fileAddressesLength = fileAddresses.Length;

        Console.WriteLine($"{fileAddressesLength} files found.");

        // 1MB buffer
        const int bufferSize = 1 * 1024 * 1024;
        Span<byte> buffer = new byte[bufferSize];
        var xxHash64 = new XxHash64();
        ulong hash;
        List<string> fileAdresses;
        var dups = new Dictionary<ulong, List<string>>();
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
            Console.WriteLine($"{index} of {fileAddressesLength}.");
        }

        Console.WriteLine(sw.Elapsed);

        // Write results in file
        FileInfo fileInfo;
        var resultFileAddress = $"{searchAddress}{Path.DirectorySeparatorChar}.UniqlySearchResult";
        using (var writer = new StreamWriter(resultFileAddress))
        {
            foreach (var (_, values) in dups.Where(e => 1 < e.Value.Count))
            {
                foreach (var value in values)
                {
                    fileInfo = new FileInfo(value);

                    writer.WriteLine($"stay/remove {value} | (Size: {FormatSize(fileInfo.Length)})");
                }

                writer.WriteLine();
            }
        }

        Console.WriteLine(resultFileAddress);

        // Open the file with the default editor
        var process = Process.Start(new ProcessStartInfo(resultFileAddress) { UseShellExecute = true });
        process.WaitForExit();

        // Read results and take action
        long deletedSize = 0;
        using (var reader = new StreamReader(resultFileAddress))
        {
            var commands = new string[2];
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                commands = line.Split(" ", 2);
                if (string.Equals(commands.First(), nameof(Enum.Remove), StringComparison.OrdinalIgnoreCase))
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
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

}
