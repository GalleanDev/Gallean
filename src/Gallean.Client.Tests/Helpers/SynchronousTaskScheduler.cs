using Gallean.Common;

namespace Gallean.Client.Tests;

internal sealed class SynchronousTaskScheduler : TaskScheduler, IStaticInstance<SynchronousTaskScheduler>
{
    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return [];
    }

    protected override void QueueTask(Task task)
    {
        TryExecuteTask(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return TryExecuteTask(task);
    }
}
