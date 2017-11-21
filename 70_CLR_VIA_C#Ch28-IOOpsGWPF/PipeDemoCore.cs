using System;
using System.Collections.Concurrent;
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
    public int TalkCount
    {
        get { return _talkCount; }
    }

    public int SlotCount
    {
        get { return _slotCount; }
    }

    private readonly int _talkCount;
    private readonly int _slotCount;
    private PipeInfo _pipeInfo;

    private SemaphoreSlim _throttler;
    private ConcurrentBag<int> _progressSlot;
    private List<int> _activeSlots;

    private ConcurrentQueue<string> _exceptionLogs=new ConcurrentQueue<string>();
    public ConcurrentQueue<string> ExceptionLogs {get{return _exceptionLogs;}}

    public PipeDemo(int talkCount=50, int slotCount=1000)
    {
        _talkCount = talkCount;
        _slotCount = slotCount;

        _pipeInfo = new PipeInfo(SlotCount);
        _throttler = new SemaphoreSlim(SlotCount);
        _progressSlot = new ConcurrentBag<int>();
        for (int i = 0; i < SlotCount; i++) { _progressSlot.Add(i); }
        _activeSlots = new List<int>();
    }


    public PipeInfo CurrentPipeInfo
    {
        get { return _pipeInfo; }
    }
    public void ResetPipeInfo()
    {
        _pipeInfo.ClearClient();
        _pipeInfo.ClearServer();
        _exceptionLogs = new ConcurrentQueue<string>();
    }

    //在我电脑上测试，3000个可以，4000个后，就会发生资源分配失败异常。


    private int isServerRunning = 0;
    public bool IsServerRunning()
    {
        return isServerRunning != 0;
    }

    CancellationTokenSource tokenSourceServer;
    List<Task> serverTasks =null;

    public bool CancelServer()
    {
        tokenSourceServer?.Cancel();
        return true;
    }

    //如果Client和Server在一个程序里面运行，则Server等待连接的时候需要Block或者Client.Connect的时候需要Block。
    //如果两边都是await，仅仅执行几十个任务也很快就会有死锁的症状（估计是namedpipe内部实现的BUG）
    //作者原始的代码中这里是异步，而客户端Connect是同步。这样的问题就是所有的任务在Create函数返回的时候已经和
    //服务端连接上了，因为客户Connect花费时间长，而这个Connect都是在主线程调用。
    //原始代码如此就会大大降低并发执行的效果。更加要命的是原始代码只是读写一次，因此基本上Create完成后，任务
    //也就完成了。
    public async Task StartServerAsync(bool pure)
    {
        if (Interlocked.Exchange(ref isServerRunning, 1) != 0)
        {
            return;
        }
        tokenSourceServer?.Dispose();
        tokenSourceServer = new CancellationTokenSource();

        _pipeInfo.ClearServer();
        serverTasks = pure? new List<Task>():null;

        while (true)
        {
            NamedPipeServerStream pipe=null;
            try { 
                pipe = new NamedPipeServerStream("PipeName", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                   PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

                Interlocked.Increment(ref _pipeInfo.sstage1);
                if (pure)
                {
                    await pipe.WaitForConnectionAsync(tokenSourceServer.Token);
                }
                else
                {
                    //如果客户端和服务器运行在同一个程序里面，则只能这样
                    pipe.WaitForConnection();
                }

                Task task = ServiceClientRequestAsync(pipe);
                serverTasks?.Add(task);
            }
            catch (OperationCanceledException e)
            {
                try {pipe?.Dispose();}catch(Exception ex){ _exceptionLogs.Enqueue(ex.Message); }
                Interlocked.Increment(ref _pipeInfo.scanceled );
            }
            catch (Exception e)
            {
                _exceptionLogs.Enqueue("Sever Main Loop: "+e.Message);
                try { pipe?.Dispose(); } catch (Exception ex) { _exceptionLogs.Enqueue(ex.Message); }
                Debug.WriteLine("服务程序异常1: @ " + e.Message);
                Interlocked.Increment(ref _pipeInfo.sexp);
            }

            if (tokenSourceServer.IsCancellationRequested)
            {
                if (serverTasks != null)
                {
                    try
                    {
                        await Task.WhenAll(serverTasks);
                    }
                    catch { }
                    serverTasks = null;
                }

                Interlocked.Exchange(ref isServerRunning, 0);
            }
        }
    }



    private async Task ServiceClientRequestAsync(NamedPipeServerStream pipe)
    {
        int currentLoop=-1;
        string currentRW = "N";

        Interlocked.Increment(ref _pipeInfo.sstage2);
        using (pipe)
        {
            try
            {
                for (int i = 0; i < _talkCount; i++)
                {
                    currentLoop = i;
                    currentRW = "R";
                    string message = await pipe.ReadAsync(1000, tokenSourceServer.Token);
                    currentRW = "W";
                    await pipe.WriteAsync(message.ToUpper(),tokenSourceServer.Token);
                    Interlocked.Increment(ref _pipeInfo.sstage3);
                }
                Interlocked.Increment(ref _pipeInfo.scompleted);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _pipeInfo.scanceled);
            }
            catch (Exception e)
            {
                string msg = "服务程序异常，循环:  " + currentLoop + ", 状态: " + currentRW + ", : " + e.Message;
                _exceptionLogs.Enqueue(msg);
                Debug.WriteLine(msg);
                Interlocked.Increment(ref _pipeInfo.sexp);
            }
            Interlocked.Increment(ref _pipeInfo.sclosed);
        }
    }


    private async Task<String> IssueClientRequestAsync(String serverName, String message)
    {
        int slot, currentLoop=-1;
        String currentRW="N";
        try
        {
            //这里Delay目的是为了让这函数立刻返回，而让调用者一下子生成成千上万个Task并发执行有意义。
            await Task.Delay(100, tokenSourceClient.Token);
            await _throttler.WaitAsync(tokenSourceClient.Token);//限制并发个数。
            if(!_progressSlot.TryTake(out slot))
            {
                throw new Exception("BUG found: IssueClientRequestAsync.1");
            }
            _pipeInfo.cProgress[slot] = 0;
            lock(_activeSlots)
                _activeSlots.Add(slot);
        }
        catch(OperationCanceledException)
        {
            Interlocked.Increment(ref _pipeInfo.ccanceled);
            return null;
        }

        try
        {
            using (var pipe = new NamedPipeClientStream(serverName, "PipeName", PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                Interlocked.Increment(ref _pipeInfo.cstage1);

                //原始代码用的是同步Connect，而没有函数第一句的Delay。因此任务是顺序而非并发执行的。
                await pipe.ConnectAsync(tokenSourceClient.Token);
                _pipeInfo.cProgress[slot] = 1.0/ (_talkCount+1.0);

                pipe.ReadMode = PipeTransmissionMode.Message;
                Interlocked.Increment(ref _pipeInfo.cstage2);

                string lastmessage = null;
                for (int i = 0; i < _talkCount; i++)
                {
                    currentLoop = i;
                    currentRW = "W";
                    await pipe.WriteAsync(message, tokenSourceClient.Token);
                    _pipeInfo.cProgress[slot] = (1+ i +  0.5) / (_talkCount+1);

                    currentRW = "R";
                    lastmessage = await pipe.ReadAsync(1000, tokenSourceClient.Token);
                    _pipeInfo.cProgress[slot] = (1+ i + 1) / (_talkCount+1);

                    Interlocked.Increment(ref _pipeInfo.cstage3);
                    //原始代码没有50循环，因此2-3就是当前在这个状态的任务的个数；有循环后，这个差没有意义了。
                }
                Interlocked.Increment(ref _pipeInfo.ccompleted);
                return lastmessage;
            }  // Close the pipe
        }
        catch(OperationCanceledException e)
        {
            Interlocked.Increment(ref _pipeInfo.ccanceled);
            return null;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _pipeInfo.cexp);
            string msg = "客户程序异常： 循环: " + currentLoop + ", 状态: " + currentRW + ", @ " + ex.Message;
            _exceptionLogs.Enqueue(msg);
            Debug.WriteLine(msg);
            return null;
        }
        finally
        {
            Interlocked.Increment(ref _pipeInfo.cclosed);
            lock (_activeSlots)
                _activeSlots.Remove(slot);
            _progressSlot.Add(slot);//归还回去
            _throttler.Release();
        }
    }

    public double[] GetActiveSlotsProgress()
    {
        int[] activeSlots;
        lock (_activeSlots)
            activeSlots = _activeSlots.ToArray();

        //这个实现只是供显示用，因此没有保证镜像准确。可能会存在数据不同时的情况。
        double[] progress = new double[activeSlots.Length ];
        for(int i=0;i< activeSlots.Length; i++)
        {
            progress[i] = _pipeInfo.cProgress[activeSlots[i]];
        }

        return progress;
    }
}


