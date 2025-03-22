using System.Diagnostics;
using System.IO.Hashing;

namespace Uniqly.Ui.Cli;
internal static class Handler
{
    private static readonly string[] _args = [Const.FindDuplicates, "D:\\WithDups"];// E:\\Phone
                                                                                    //Environment.GetCommandLineArgs();

    internal static void FindDuplicates()
    {
        var sw = Stopwatch.StartNew();

        var fileAddresses = Directory.GetFiles(_args[1], "*", SearchOption.AllDirectories);
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
        foreach (var fileAddress in fileAddresses)
        {
            using (var stream = File.OpenRead(fileAddress))
            {
                int bytesRead;

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
    }
}
