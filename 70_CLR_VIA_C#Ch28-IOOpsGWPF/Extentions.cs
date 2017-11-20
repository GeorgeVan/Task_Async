using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


static class TobeDeleted
{
    static async Task<T> DisableSysDebugOnCancel<T>(this Task<T> task, T canceledValue, T faultedValue)
    {
        return await task.ContinueWith(
            (t) =>
            {
                if (t.IsCanceled) return canceledValue;
                else if (t.IsFaulted) return faultedValue;
                else return t.Result;
            },
            TaskContinuationOptions.ExecuteSynchronously);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Causes compiler to optimize the call away
    static async Task DisableSysDebugOnCancel(this Task task)
    {
        await task.ContinueWith((t) => { },
            TaskContinuationOptions.ExecuteSynchronously);
    }


}
public static class PipeDemoExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Causes compiler to optimize the call away
    public static async Task<string> ReadAsync(this Stream pipe, int maxLength, CancellationToken token)
    {
        Byte[] response = new Byte[maxLength];
        int bytesRead = await pipe.ReadAsync(response, 0, response.Length, token);
        return Encoding.UTF8.GetString(response, 0, bytesRead);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Causes compiler to optimize the call away
    public static async Task WriteAsync(this Stream pipe, string message, CancellationToken token)
    {
        Byte[] request = Encoding.UTF8.GetBytes(message);
        await pipe.WriteAsync(request, 0, request.Length, token);
    }

    private static async Task WaitForClientToCompleteAsync(Task<String>[] requests)
    {
#if true   // Continue AFTER ALL tasks complete
        // Asynchronously wait until all client requests have completed
        await Task.WhenAll(requests);
#endif
#if false   // Continue AS EACH task completes
        List<Task<String>> pendingRequests = new List<Task<String>>(requests);
        while (pendingRequests.Count > 0)
        {
            // Asynchronously wait until a client request has completed
            Task<String> response = await Task.WhenAny(pendingRequests).ConfigureAwait(false);
            pendingRequests.Remove(response); // Remove completed Task from collection

            // Process the response
            Console.WriteLine(response.Result);
            //这个也没有任何意义在于我们在创建每个Task的时候就可以使用Continue来打印，而不需要这么再折腾一次。
        }
#endif
#if false   // More efficient way to continue AS EACH task completes
        foreach (var t in WhenEach(requests))
        {
            // Asynchronously wait until the next client request has completed
            Task<String> response = await t.ConfigureAwait(false);
            Interlocked.Increment(ref _pipeInfo.ccompleted1);
            // Process the response
            //Console.WriteLine(response.Result);
            //这个也没有任何意义在于我们在创建每个Task的时候就可以使用Continue来打印，而不需要这么再折腾一次。
        }
#endif
    }

   
}

public partial class PipeDemo
{
    private IEnumerable<Task<Task<TResult>>> WhenEach<TResult>(params Task<TResult>[] tasks)
    {
        // Create a new TaskCompletionSource for each task
        var taskCompletions = new TaskCompletionSource<Task<TResult>>[tasks.Length];

        Int32 next = -1;  // Identifies the next TaskCompletionSource to complete
                          // As each task completes, this callback completes the next TaskCompletionSource
        Action<Task<TResult>> taskCompletionCallback = t => {
            taskCompletions[Interlocked.Increment(ref next)].SetResult(t);
            Interlocked.Increment(ref _pipeInfo.ccompleted2);
        };

        // Create all the TaskCompletionSource objects and tell each task to 
        // complete the next one as each task completes
        for (Int32 n = 0; n < tasks.Length; n++)
        {
            taskCompletions[n] = new TaskCompletionSource<Task<TResult>>();
            tasks[n].ContinueWith(taskCompletionCallback, TaskContinuationOptions.RunContinuationsAsynchronously);
            //原始代码是让Callback同步执行，后来发现其实任务已经完成了，但是这个包装的任务一直不完成。
            //修改为异步执行，就OK了。
        }
        // Return each of the TaskCompledtionSource's Tasks in turn.
        // The Result property represents the original task that completed.
        for (Int32 n = 0; n < tasks.Length; n++) yield return taskCompletions[n].Task;
    }
}
