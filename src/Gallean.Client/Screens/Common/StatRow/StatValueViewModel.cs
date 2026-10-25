namespace Gallean.Client;

internal sealed record StatValueViewModel(TextSpans Text, int Change)
{
    public string ChangeText => Change == 0 ? string.Empty : $" {Change:+0;-0}";
}
