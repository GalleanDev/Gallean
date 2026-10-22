using System.Runtime.ExceptionServices;
using Avalonia.Threading;

namespace Gallean.Client;

internal static class TaskExtensions
{
    public static void FileAndForget(this Task task)
    {
        task.ContinueWith(
            static faulted =>
            {
                Exception ex = faulted.Exception!.GetBaseException();
                Dispatcher.UIThread.Post(() => ExceptionDispatchInfo.Capture(ex).Throw());
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
