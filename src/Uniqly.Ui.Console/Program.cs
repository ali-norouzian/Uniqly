using Uniqly.Ui.Cli;

args = [Const.FindDuplicates, "D:\\WithDups"];// E:\\Phone

switch (args[0])
{
    case Const.FindDuplicates:
        Handler.FindDuplicates();
        break;

}

Console.ReadKey();
