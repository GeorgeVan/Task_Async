﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

// Demonstrates how to provide delegates to exectution dataflow blocks.
class DataflowExecutionBlocks
{
    // Computes the number of zero bytes that the provided file
    // contains.
    static int CountBytes(string path)
    {
        byte[] buffer = new byte[1024];
        int totalZeroBytesRead = 0;
        using (var fileStream = File.OpenRead(path))
        {
            int bytesRead = 0;
            do
            {
                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                totalZeroBytesRead += buffer.Count(b => b == 0);
            } while (bytesRead > 0);
        }

        return totalZeroBytesRead;
    }

    static void Main(string[] args)
    {
        // Create a temporary file on disk.
        string tempFile = Path.GetTempFileName();

        // Write random data to the temporary file.
        using (var fileStream = File.OpenWrite(tempFile))
        {
            Random rand = new Random();
            byte[] buffer = new byte[1024];
            for (int i = 0; i < 512; i++)
            {
                rand.NextBytes(buffer);
                fileStream.Write(buffer, 0, buffer.Length);
            }
        }

        // Create an ActionBlock<int> object that prints to the console 
        // the number of bytes read.
        var printResult = new ActionBlock<int>(zeroBytesRead =>
        {
            Console.WriteLine("{0} contains {1} zero bytes.",
               Path.GetFileName(tempFile), zeroBytesRead);
        });

        // Create a TransformBlock<string, int> object that calls the 
        // CountBytes function and returns its result.
        var countBytes = new TransformBlock<string, int>(
           new Func<string, int>(CountBytes));

        // Link the TransformBlock<string, int> object to the 
        // ActionBlock<int> object.
        countBytes.LinkTo(printResult);

        // Create a continuation task that completes the ActionBlock<int>
        // object when the TransformBlock<string, int> finishes.
       // countBytes.Completion.ContinueWith(delegate { printResult.Complete(); });
        //G将这个注释掉，用下面那个

        // Post the path to the temporary file to the 
        // TransformBlock<string, int> object.
        countBytes.Post(tempFile);

        // Requests completion of the TransformBlock<string, int> object.
        countBytes.Complete();

        //GG
        countBytes.Completion.Wait();
        printResult.Complete();
        //GG
    
        // Wait for the ActionBlock<int> object to print the message.
        printResult.Completion.Wait();

        // Delete the temporary file.
        File.Delete(tempFile);
        Console.ReadKey();
    }
}

/* Sample output:
tmp4FBE.tmp contains 2081 zero bytes.
*/
