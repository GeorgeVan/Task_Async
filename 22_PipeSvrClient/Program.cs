using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _22_PipeSvrClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Go().GetAwaiter().GetResult();
        }

        public static async Task Go()
        {
            // Start the server which returns immediately since
            // it asynchronously waits for client requests
            StartServer(); // This returns void, so compiler warning to deal with
                           // Make lots of async client requests; save each client's Task<String>
            List<Task<String>> requests = new List<Task<String>>(10000);
            for (Int32 n = 0; n < requests.Capacity; n++)
                requests.Add(IssueClientRequestAsync("localhost", "Request #" + n));
            // Asynchronously wait until all client requests have completed
            // NOTE: If 1+ tasks throws, WhenAll rethrows the last-throw exception
            String[] responses = await Task.WhenAll(requests);
            // Process all the responses
            for (Int32 n = 0; n < responses.Length; n++)
                Console.WriteLine(responses[n]);
        }

        public static async Task Go1()
        {
            // Start the server which returns immediately since
            // it asynchronously waits for client requests
            StartServer();
            // Make lots of async client requests; save each client's Task<String>
            List<Task<String>> requests = new List<Task<String>>(10000);
            for (Int32 n = 0; n < requests.Capacity; n++)
                requests.Add(IssueClientRequestAsync("localhost", "Request #" + n));
            // Continue AS EACH task completes
            while (requests.Count > 0)
            {
                // Process each completed response sequentially
                Task<String> response = await Task.WhenAny(requests);
                requests.Remove(response); // Remove the completed task from the collection
                                           // Process a single client's response
                Console.WriteLine(response.Result);
            }
        }

        private static async Task<String> IssueClientRequestAsync(String serverName, String message)
        {
            using (var pipe = new NamedPipeClientStream(serverName, "PipeName", PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                pipe.Connect(); // Must Connect before setting ReadMode
                pipe.ReadMode = PipeTransmissionMode.Message;
                // Asynchronously send data to the server
                Byte[] request = Encoding.UTF8.GetBytes(message);
                await pipe.WriteAsync(request, 0, request.Length);
                // Asynchronously read the server's response
                Byte[] response = new Byte[1000];
                Int32 bytesRead = await pipe.ReadAsync(response, 0, response.Length);
                return Encoding.UTF8.GetString(response, 0, bytesRead);
            } // Close the pipe
        }

        private static async void StartServer()
        {
            while (true)
            {
                var pipe = new NamedPipeServerStream(c_pipeName, PipeDirection.InOut, -1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
                // Asynchronously accept a client connection
                // NOTE: NamedPipServerStream uses the old Asynchronous Programming Model (APM)
                // I convert the old APM to the new Task model via TaskFactory's FromAsync method
                await Task.Factory.FromAsync(pipe.BeginWaitForConnection, pipe.EndWaitForConnection, null);
                // Start servicing the client which returns immediately since it is asynchronous
                ServiceClientRequestAsync(pipe);
            }
        }

    }
}
