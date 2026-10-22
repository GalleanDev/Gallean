namespace Gallean.Client;

internal interface ILog
{
    string? Location { get; }

    void Write(string text);
}
