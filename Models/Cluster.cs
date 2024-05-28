using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Cluster
{
    public WorkerNode[] WorkerNodes { get; private set; }
    private int dimensions;

    public Cluster(int dimensions)
    {
        this.dimensions = dimensions;
        int totalWorkerNodes = (int)Math.Pow(2, dimensions);
        WorkerNodes = new WorkerNode[totalWorkerNodes];

        for (int i = 0; i < totalWorkerNodes; i++)
        {
            WorkerNodes[i] = new WorkerNode(i);
        }
    }

    public WorkerNode GetWorkerNode(int id)
    {
        return WorkerNodes[id];
    }

    public double GetAverageLoad()
    {
        return WorkerNodes.Average(s => s.TotalLoad);
    }

    public double GetLoadDeviationPercentage()
    {
        double averageLoad = GetAverageLoad();
        if (averageLoad == 0)
        {
            return 0;
        }

        double totalDeviation = WorkerNodes.Sum(s => Math.Abs(s.TotalLoad - averageLoad));

        return (totalDeviation / (WorkerNodes.Length * averageLoad)) * 100;
    }

    public async Task<int> ProcessAllTasksAsync()
    {
        var tasks = WorkerNodes.Select(workerNode => ProcessWorkerNodeTasksAsync(workerNode)).ToArray();
        await Task.WhenAll(tasks);
        return tasks.Sum(t => t.Result);
    }

    private async Task<int> ProcessWorkerNodeTasksAsync(WorkerNode workerNode)
    {
        int totalProcessingTime = 0;
        while (workerNode.Queue.Any())
        {
            totalProcessingTime += await workerNode.ProcessTaskAsync();
        }
        return totalProcessingTime;
    }

    public async Task<int> SimulateProcessingWithoutDistributionAsync(int[] taskTimes)
    {
        WorkerNode workerNode = WorkerNodes[0];
        SemaphoreSlim semaphore = new SemaphoreSlim(5, 5); // Maximum 5 tasks

        foreach (var taskTime in taskTimes)
        {
            await semaphore.WaitAsync();

            Task.Run(async () =>
            {
                workerNode.AddTask(taskTime);
                await workerNode.ProcessTaskAsync();
                semaphore.Release();
            });
        }

        // Wait for all tasks to be processed
        while (semaphore.CurrentCount < 5)
        {
            await Task.Delay(10);
        }

        return workerNode.TotalLoad;
    }

    public void PrintWorkerNodeStatistics()
    {
        foreach (var workerNode in WorkerNodes)
        {
            Console.WriteLine($"WorkerNode {workerNode.Id}: TasksProcessed = {workerNode.TasksProcessed}, TotalLoad = {workerNode.TotalLoad}");
        }
    }

    public void PrintFirstWorkerNodeStatistics()
    { 
        Console.WriteLine($"WorkerNode {WorkerNodes[0].Id}: TasksProcessed = {WorkerNodes[0].TasksProcessed}, TotalLoad = {WorkerNodes[0].TotalLoad}");
        
    }
}
