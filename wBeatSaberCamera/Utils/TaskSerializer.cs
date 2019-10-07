using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wBeatSaberCamera.Utils
{
    public class TaskSerializer
    {
        private readonly Dictionary<string, Queue<Func<Task>>> _taskQueue = new Dictionary<string, Queue<Func<Task>>>();

        public void Enqueue(string key, Func<Task> taskToExecute)
        {
            lock (_taskQueue)
            {
                if (_taskQueue.TryGetValue(key, out var queue))
                {
                    queue.Enqueue(taskToExecute);
                    return;
                }
                var newQueue = new Queue<Func<Task>>();
                newQueue.Enqueue(taskToExecute);
                _taskQueue.Add(key, newQueue);
                RunQueueProcessors(key, newQueue);
            }
        }

        private void RunQueueProcessors(string key, Queue<Func<Task>> queue)
        {
            Task.Run(async () =>
            {
                do
                {
                    var item = queue.Dequeue();
                    await item();
                }
                while (queue.Count > 0);

                lock (_taskQueue)
                {
                    if (queue.Count > 0)
                    {
                        RunQueueProcessors(key, queue);
                    }
                    else
                    {
                        _taskQueue.Remove(key);
                    }
                }
            });
        }
    }
}