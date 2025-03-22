using Uniqly.Ui.Cli;

args = [Const.FindDuplicates, "D:\\WithDups"];// E:\\Phone

switch (args[0])
{
    case Const.FindDuplicates:
        Handler.FindDuplicates();
        break;

}


await Task.Delay(1000000000);
