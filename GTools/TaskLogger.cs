using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GTools
{
    public static class TaskLogger
    {
        public enum TaskLogLevel { None, Pending }
        public static TaskLogLevel LogLevel { get; set; }
        public sealed class TaskLogEntry
        {
            public Task Task { get; internal set; }
            public String Tag { get; internal set; }
            public DateTime LogTime { get; internal set; }
            public String CallerMemberName { get; internal set; }
            public String CallerFilePath { get; internal set; }
            public Int32 CallerLineNumber { get; internal set; }
            public override string ToString()
            {
                return String.Format("LogTime={0}, Tag={1}, Member={2}, File={3}({4})",
                LogTime, Tag ?? "(none)", CallerMemberName, CallerFilePath, CallerLineNumber);
            }
        }
        private static readonly ConcurrentDictionary<Task, TaskLogEntry> s_log =
        new ConcurrentDictionary<Task, TaskLogEntry>();
        public static IEnumerable<TaskLogEntry> GetLogEntries() { return s_log.Values; }
        public static Task<TResult> Log<TResult>(this Task<TResult> task, String tag = null,
        [CallerMemberName] String callerMemberName = null,
        [CallerFilePath] String callerFilePath = null,
        [CallerLineNumber] Int32 callerLineNumber = -1)
        {
            return (Task<TResult>)Log((Task)task, tag, callerMemberName, callerFilePath, callerLineNumber);
        }
        public static Task Log(this Task task, String tag = null,
        [CallerMemberName] String callerMemberName = null,
        [CallerFilePath] String callerFilePath = null,
        [CallerLineNumber] Int32 callerLineNumber = -1)
        {
            if (LogLevel == TaskLogLevel.None) return task;
            var logEntry = new TaskLogEntry
            {
                Task = task,
                LogTime = DateTime.Now,
                Tag = tag,
                CallerMemberName = callerMemberName,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber
            };
            s_log[task] = logEntry;
            task.ContinueWith(t => { TaskLogEntry entry; s_log.TryRemove(t, out entry); },
            TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        public static async Task Go()
        {
#if DEBUG
            // Using TaskLogger incurs a memory and performance hit; so turn it on in debug builds
            TaskLogger.LogLevel = TaskLogger.TaskLogLevel.Pending;
#endif
            // Initiate 3 task; for testing the TaskLogger, we control their duration explicitly
            var tasks = new List<Task> {
                Task.Delay(2000).Log("2s op"),
                Task.Delay(5000).Log("5s op"),
                Task.Delay(6000).Log("6s op")};
            try
            {
                // Wait for all tasks but cancel after 3 seconds; only 1 task above should complete in time
                // Note: WithCancellation is my extension method described later in this chapter
                await Task.WhenAll(tasks).
                WithCancellation(new CancellationTokenSource(3000).Token);
            }
            catch (OperationCanceledException) { }
            // Ask the logger which tasks have not yet completed and sort
            // them in order from the one that’s been waiting the longest
            foreach (var op in TaskLogger.GetLogEntries().OrderBy(tle => tle.LogTime))
                Console.WriteLine(op);
        }
    }


}
