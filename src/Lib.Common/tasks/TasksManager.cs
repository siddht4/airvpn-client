﻿#if EDDIENET4

using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eddie.Common.Tasks
{
    public class TasksManager : ITasksManager
    {
        private List<TaskEx> m_tasks = new List<TaskEx>();
        private object m_tasksSync = new object();

        public int Count
        {
            get
            {
                lock(m_tasksSync)
                {
                    return m_tasks.Count;
                }
            }
        }

        public TaskEx Add(Action<CancellationToken> action, bool start = true, CancellationTokenSource cancellation = null)
        {
            CancellationTokenSource cancellationSource = cancellation != null ? cancellation : new CancellationTokenSource();
            TaskEx task = null;
            task = new TaskEx(cancellationSource, () => { action(cancellationSource.Token); Remove(task); });
        
            lock(m_tasksSync)
            {
                m_tasks.Add(task);
            }

            if(start)
                task.Start();

            return task;
        }        

        public void Remove(TaskEx task)
        {
            if(task == null)
                return;

            lock(m_tasksSync)
            {
                m_tasks.Remove(task);
				task.Cleanup();

				// https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
				//task.Dispose();
			}
		}

        public void CancelAll()
        {
            TaskEx[] tasks = null;

            lock(m_tasksSync)
            {
                foreach(TaskEx t in m_tasks)
                {
                    t.cancel();
                }

                tasks = m_tasks.Count > 0 ? m_tasks.ToArray() : null;
                m_tasks.Clear();
            }

            if(tasks != null)
                Task.WaitAll(tasks);
        }

        public void WaitAll()
        {
            TaskEx[] tasks = null;

            lock(m_tasksSync)
            {
                tasks = m_tasks.Count > 0 ? m_tasks.ToArray() : null;
            }

            if(tasks != null)
                Task.WaitAll(tasks);
        }

        public void Dispose()
        {
            CancelAll();
        }
    }
}

#endif