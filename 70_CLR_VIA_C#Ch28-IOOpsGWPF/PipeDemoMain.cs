using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public partial class PipeDemo
{
    public async Task Go(int clientCount)
    {
        StartServerInner();
        await StartClientAsync(clientCount);
    }

    CancellationTokenSource tokenSourceClient;

    public bool CancelClient()
    {
        if (tokenSourceClient == null) return false;
        tokenSourceClient.Cancel();
        return true;
    }

    //启动多个线程，每个线程串行去连接。
    public async Task<TimeSpan> StartClientAsync2(int clientCount)
    {
        _pipeInfo.ClearClient();
        _pipeInfo.order = clientCount;
        tokenSourceClient?.Dispose();
        tokenSourceClient = new CancellationTokenSource();

        Stopwatch stopwatch = Stopwatch.StartNew();
        Action action = () =>
        {
            while(true)
            {
                int n = Interlocked.Decrement(ref clientCount);
                if (n < 0) break;
                IssueClientRequestAsync("localhost", "Request #" + n)
                    .ContinueWith(delegate { Interlocked.Increment(ref _pipeInfo.ccompleted); },
                                                    TaskContinuationOptions.ExecuteSynchronously | 
                                                    TaskContinuationOptions.OnlyOnRanToCompletion)
                    .Wait();
            }
        };

        await Task.WhenAll(Enumerable.Range(1,8).Select((n)=>Task.Run(action)).ToArray());
        tokenSourceClient.Dispose();
        tokenSourceClient = null;
        return stopwatch.Elapsed;
    }

    public async Task<TimeSpan> StartClientAsync(int clientCount) {
        _pipeInfo.ClearClient();
        _pipeInfo.order = clientCount;
        tokenSourceClient?.Dispose();
        tokenSourceClient = new CancellationTokenSource();
        Stopwatch stopwatch = Stopwatch.StartNew();

        Debug.WriteLine("StartClientAsync " + Thread.CurrentThread.ManagedThreadId);

        // Make lots of async client requests; save each client's Task<String>
        Task<String>[] requests = new Task<String>[clientCount];
        for (Int32 n = 0; n < requests.Length; n++) {
            Interlocked.Increment(ref _pipeInfo.created);
            requests[n] = IssueClientRequestAsync("localhost", "Request #" + n);
        }
        //await WaitForClientToCompleteAsync(requests);

        try
        {
            await Task.WhenAll(requests);
        }
        catch { }

        tokenSourceClient.Dispose();
        tokenSourceClient = null;
        return stopwatch.Elapsed;
    }

    private void StartServerInner()
    {
        Task.Run(()=> StartServerAsync(false));
    }

}