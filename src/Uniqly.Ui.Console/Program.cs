using Uniqly.Ui.Cli;

//args = [Command.FindDuplicates, "D:\\WithDups"];// E:\\Phone
args = [Command.ApplyChanges, "D:\\WithDups", Command.KeepNewest];// E:\\Phone

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case Command.FindDuplicates:
            Handler.FindDuplicates();
            break;
        case Command.ApplyChanges:
            switch (args[i + 2])
            {
                case Command.KeepNewest:
                    Handler.KeepNewestAndDeleteOthers();
                    break;
            }
            break;
    }

}

await Task.Delay(1000000000);
