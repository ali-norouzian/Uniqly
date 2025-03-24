using Uniqly.Ui.Cli;

//args = [Command.FindDuplicates, "D:\\WithDups"];// E:\\Phone
//args = [Command.ApplyChanges, "D:\\WithDups", Command.KeepNewest];// E:\\Phone

args = Environment.GetCommandLineArgs();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case var arg when arg.Equals(Command.Search, StringComparison.OrdinalIgnoreCase):
            Handler.FindDuplicates(i + 1);
            return;
        case var arg when arg.Equals(Command.Apply, StringComparison.OrdinalIgnoreCase):
            for (var j = i; j < args.Length; j++)
            {
                switch (args[j])
                {
                    case var subArg when
                       subArg.Equals(Command.KeepNewest, StringComparison.OrdinalIgnoreCase) ||
                       subArg.Equals(Command.KN, StringComparison.OrdinalIgnoreCase):
                        Handler.KeepNewestAndDeleteOthers(i + 1);
                        return;
                }
            }

            // There is no flag. do the default behavior
            Handler.DoTheDefaultApplyBehavior(i + 1);

            return;
        case var arg when
            arg.Equals(Command.Version, StringComparison.OrdinalIgnoreCase) ||
            arg.Equals(Command.V, StringComparison.OrdinalIgnoreCase):
            Handler.Version();
            return;
        case var arg when
            arg.Equals(Command.Help, StringComparison.OrdinalIgnoreCase) ||
            arg.Equals(Command.H, StringComparison.OrdinalIgnoreCase):
            Handler.Help();
            return;
    }
}

Console.WriteLine("Invalid command! look at help document:");
Console.WriteLine();
Handler.Help();

//await Task.Delay(1000000000);
