using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class GradientLoadBalancer
{
    private Cluster cluster;
    private int dimensions;

    public GradientLoadBalancer(Cluster cluster, int dimensions)
    {
        this.cluster = cluster;
        this.dimensions = dimensions;
    }

    private IEnumerable<int> GetNeighbors(int id)
    {
        for (int d = 0; d < dimensions; d++)
        {
            yield return id ^ (1 << d);
        }
    }

    public async Task AddTaskAsync(int taskTime)
    {
        while (true)
        {
            var workerNode = cluster.WorkerNodes.OrderBy(s => s.QueueLength).First();
            if (!workerNode.IsQueueFull)
            {
                workerNode.AddTask(taskTime);
                break;
            }
            await Task.Delay(10); // wait a bit before trying again
        }
    }

    public async Task BalanceLoadAsync()
    {
        while (cluster.WorkerNodes.Any(w => w.Queue.Any()))
        {
            double averageLoad = cluster.GetAverageLoad();
            bool loadBalanced = true;

            foreach (var workerNode in cluster.WorkerNodes)
            {
                double workerNodeLoad = workerNode.QueueLength;
                double gradient = workerNodeLoad - averageLoad;

                if (gradient > 0)
                {
                    var neighbors = GetNeighbors(workerNode.Id)
                        .Select(id => cluster.GetWorkerNode(id))
                        .Where(s => s.QueueLength < averageLoad)
                        .OrderBy(s => s.QueueLength)
                        .ToList();

                    foreach (var neighbor in neighbors)
                    {
                        if (workerNode.Queue.TryDequeue(out int task))
                        {
                            neighbor.AddTask(task);
                            loadBalanced = false;
                            break;
                        }
                    }
                }
            }

            if (loadBalanced)
            {
                break;
            }

            await Task.Delay(10); // Delay to simulate real-world scenario and to prevent tight loop
        }
    }
}
