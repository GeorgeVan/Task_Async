using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

// Demonstrates how to use non-greedy join blocks to distribute
// resources among a dataflow network.
class Program
{
    // Represents a resource. A derived class might represent 
    // a limited resource such as a memory, network, or I/O
    // device.
    abstract class Resource
    {
    }

    // Represents a memory resource. For brevity, the details of 
    // this class are omitted.
    class MemoryResource : Resource
    {
    }

    // Represents a network resource. For brevity, the details of 
    // this class are omitted.
    class NetworkResource : Resource
    {
    }

    // Represents a file resource. For brevity, the details of 
    // this class are omitted.
    class FileResource : Resource
    {
    }

    static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static GroupingDataflowBlockOptions nonGreedy = new GroupingDataflowBlockOptions
    {
        Greedy = false,
        BoundedCapacity =1
        //MaxMessagesPerTask = 3
    };

    static ExecutionDataflowBlockOptions degreeOfParall = new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = 2
    };

    static bool useAsync = false;

static Random random = new Random();

    async static Task MainAsync() { 
        var networkResources = new BufferBlock<NetworkResource>();
        var fileResources = new BufferBlock<FileResource>();
        var memoryResources = new BufferBlock<MemoryResource>();

        var joinNetworkAndMemoryResources = new JoinBlock<NetworkResource, MemoryResource>(nonGreedy);
        var joinFileAndMemoryResources = new JoinBlock<FileResource, MemoryResource>(nonGreedy);

        var networkMemoryAction =
           new ActionBlock<Tuple<NetworkResource, MemoryResource>>( async(data) =>
              {
                  Console.WriteLine("Network worker: using resources... @{0}",Thread.CurrentThread.ManagedThreadId );
                  if (useAsync)
                  {
                      await Task.Delay(random.Next(500, 2000)).ConfigureAwait(false);
                  }
                  else
                  {
                      Thread.Sleep(random.Next(500, 2000));
                  }
                  Console.WriteLine("Network worker: finished using resources...@{0}", Thread.CurrentThread.ManagedThreadId);
                  memoryResources.Post(data.Item2);
                  //await Task.Delay(random.Next(1000)).ConfigureAwait(false);
                  //这里如果不Delay的话会经常发生某种工人一直在执行的情况

                  networkResources.Post(data.Item1);
              }, degreeOfParall);

        var fileMemoryAction =
           new ActionBlock<Tuple<FileResource, MemoryResource>>(async (data) =>
              {
                  Console.WriteLine("File worker: using resources...@{0}", Thread.CurrentThread.ManagedThreadId);
                  if (useAsync)
                  {
                      await Task.Delay(random.Next(500, 2000)).ConfigureAwait(false);
                  }
                  else
                  {
                      Thread.Sleep(random.Next(500, 2000));
                  }
                  Console.WriteLine("File worker: finished using resources...@{0}", Thread.CurrentThread.ManagedThreadId);
                  memoryResources.Post(data.Item2);
                  //await Task.Delay(random.Next(1000)).ConfigureAwait(false);
                  //这里如果不Delay的话会经常发生某种工人一直在执行的情况

                  fileResources.Post(data.Item1);
              }, degreeOfParall);

        fileResources.LinkTo(joinFileAndMemoryResources.Target1);
        networkResources.LinkTo(joinNetworkAndMemoryResources.Target1);

        memoryResources.LinkTo(joinNetworkAndMemoryResources.Target2);
        memoryResources.LinkTo(joinFileAndMemoryResources.Target2);

        joinNetworkAndMemoryResources.LinkTo(networkMemoryAction);
        joinFileAndMemoryResources.LinkTo(fileMemoryAction);


        fileResources.Post(new FileResource());
        fileResources.Post(new FileResource());
        fileResources.Post(new FileResource());

        networkResources.Post(new NetworkResource());
        networkResources.Post(new NetworkResource());
        networkResources.Post(new NetworkResource());

        memoryResources.Post(new MemoryResource());
        memoryResources.Post(new MemoryResource());

        await memoryResources.Completion;
    }
}

/* Sample output:
File worker: using resources...
File worker: finished using resources...
Network worker: using resources...
Network worker: finished using resources...
File worker: using resources...
File worker: finished using resources...
Network worker: using resources...
Network worker: finished using resources...
File worker: using resources...
File worker: finished using resources...
File worker: using resources...
File worker: finished using resources...
Network worker: using resources...
Network worker: finished using resources...
Network worker: using resources...
Network worker: finished using resources...
File worker: using resources...
*/
