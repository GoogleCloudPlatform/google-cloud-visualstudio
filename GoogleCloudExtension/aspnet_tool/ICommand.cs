namespace AspnetTool
{
    public interface ICommand
    {
        int Execute();
    }

    public interface ICommandOptions
    {
        ICommand CreateCommand();
    }
}
