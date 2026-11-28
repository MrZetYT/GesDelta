namespace GesDelta.Orchestrator;

public static class RoundRobin
{
    public static IEnumerable<T> Interleave<T>(
        IEnumerable<IEnumerable<T>> groups)
    {
        var queues = groups
            .Select(group => new Queue<T>(group))
            .Where(queue => queue.Count > 0)
            .ToArray();

        var active = new Queue<Queue<T>>(queues);

        while (active.Count > 0)
        {
            var queue = active.Dequeue();

            yield return queue.Dequeue();

            if (queue.Count > 0)
                active.Enqueue(queue);
        }
    }
}