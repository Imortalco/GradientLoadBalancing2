using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class WorkerNode
{
    public int Id { get; }
    public ConcurrentQueue<int> Queue { get; }
    public int TasksProcessed { get; private set; }
    public int TotalLoad { get; private set; }

    private SemaphoreSlim semaphore = new SemaphoreSlim(5, 5); // Maximum 5 tasks

    public WorkerNode(int id)
    {
        Id = id;
        Queue = new ConcurrentQueue<int>();
    }

    public bool IsQueueFull => Queue.Count >= 5;

    public int QueueLength => Queue.Count;

    public void AddTask(int taskTime)
    {
        Queue.Enqueue(taskTime);
    }

    public async Task<int> ProcessTaskAsync()
    {
        await semaphore.WaitAsync();
        if (Queue.TryDequeue(out int taskTime))
        {
            await Task.Delay(taskTime * 100); // Simulate task processing time
            TasksProcessed++;
            TotalLoad += taskTime;
        }
        semaphore.Release();
        return taskTime;
    }
}
