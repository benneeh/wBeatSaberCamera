using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wBeatSaberCamera.Utils
{
    public class TaskSerializer
    {
        private readonly Dictionary<string, Queue<Func<Task>>> _taskQueue = new Dictionary<string, Queue<Func<Task>>>();

        public Task Enqueue(string key, Func<Task> taskToExecute)
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tc = taskToExecute;
            taskToExecute = async () =>
            {
                await tc();
                taskCompletionSource.SetResult(null);
            };
            
            lock (_taskQueue)
            {
                if (_taskQueue.TryGetValue(key, out var queue))
                {
                    queue.Enqueue(taskToExecute);
                    return taskCompletionSource.Task;
                }
                var newQueue = new Queue<Func<Task>>();
                newQueue.Enqueue(taskToExecute);
                _taskQueue.Add(key, newQueue);
                RunQueueProcessors(key, newQueue);
            }

            return taskCompletionSource.Task;
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