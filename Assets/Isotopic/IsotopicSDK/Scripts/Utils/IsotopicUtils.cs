using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace IsotopicSDK.Utils
{
    public static class IsoUtils
    {
        /// <summary>
        /// Sets some asynchronous methods to be run in parallel, limiting the amount of concurrently running tasks. 
        /// </summary>
        /// <param name="tasks">A list of tasks to be ran.</param>
        /// <param name="parallelMax">Max amount of tasks to run concurrently.</param>
        /// <param name="cb">Callback to call once all tasks finish.</param>
        public static void RunQueuedTasks(IList<Action<Action>> tasks, int parallelMax, Action cb)
        {
            if (tasks.Count == 0)
            {
                cb?.Invoke();
                return;
            }

            List<Action<Action>> finishedIndeces = new List<Action<Action>>();
            List<Action<Action>> runningIndeces = new List<Action<Action>>();

            Action OnTaskFinish(Action<Action> task) => () =>
            {
                finishedIndeces.Add(task);
                runningIndeces.Remove(task);
                Action<Action> next = tasks.FirstOrDefault(t => !finishedIndeces.Contains(t) && !runningIndeces.Contains(t));
                next?.Invoke(OnTaskFinish(next));
                if (finishedIndeces.Count == tasks.Count)
                {
                    cb?.Invoke();
                }
            };

            for (int i=0; i<parallelMax && i<tasks.Count; i++)
            {
                runningIndeces.Add(tasks[i]);
                tasks[i]?.Invoke(OnTaskFinish(tasks[i]));
            }
        }

        /// <summary>
        /// Runs provided tasks in parallel.
        /// </summary>
        /// <param name="tasks">Tasks to run.</param>
        /// <param name="cb">Callback to call once all tasks finish.</param>
        public static void RunTasksParallel(IList<Action<Action>> tasks, Action cb)
        {
            if (tasks.Count == 0)
            {
                cb?.Invoke();
                return;
            }

            int completed = 0;
            foreach (var task in tasks)
            {
                task?.Invoke(() =>
                {
                    completed++;
                    if (tasks.Count == completed)
                    {
                        cb?.Invoke();
                    }
                });
            }
        }

    }
}

