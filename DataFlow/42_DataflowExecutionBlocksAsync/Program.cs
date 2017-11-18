using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace _42_DataflowExecutionBlocksAsync
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
            Console.ReadKey();
        }

        async static Task WriteTempFileSync(string tempFile)
        {
            // Write random data to the temporary file.
            using (var fileStream = File.OpenWrite(tempFile))
            {
                Random rand = new Random();
                byte[] buffer = new byte[1024];
                for (int i = 0; i < 512; i++)
                {
                    rand.NextBytes(buffer);
                    await fileStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                }
            }
        }

        async static Task MainAsync()
        {
            string tempFile = Path.GetTempFileName();

            await WriteTempFileSync(tempFile);

            var countBytesAsync = new TransformBlock<string, int>(async path =>
            {
                byte[] buffer = new byte[1024];
                int totalZeroBytesRead = 0;
                using (var fileStream = new FileStream(
                   path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, true))
                {
                    int bytesRead = 0;
                    do
                    {
                        // Asynchronously read from the file stream.
                        bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                        totalZeroBytesRead += buffer.Count(b => b == 0);
                    } while (bytesRead > 0);
                }

                return totalZeroBytesRead;
            });

            var printResult = new ActionBlock<int>(zeroBytesRead =>
            {
                Console.WriteLine("{0} contains {1} zero bytes.",
                   Path.GetFileName(tempFile), zeroBytesRead);
            });

            countBytesAsync.LinkTo(printResult);

            countBytesAsync.Post(tempFile);
            countBytesAsync.Complete();
            await countBytesAsync.Completion;

            printResult.Complete();
            await printResult.Completion;

            File.Delete(tempFile);
        }
    }
}
