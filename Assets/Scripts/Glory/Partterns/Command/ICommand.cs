

public interface ICommand
{
    void Execute();
    void Update();
    void Cancel();

    bool IsFinished();
}
