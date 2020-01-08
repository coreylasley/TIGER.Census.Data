using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Tiger.Extract;
using static Tiger.Helper.Enums;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {

            //ActionBlockTest();

            List<DataTypes> currentlyProcessing = new List<DataTypes>();
            List<DataTypes> doneProcessing = new List<DataTypes>();
            List<DataTypes> toProcess = Enum.GetValues(typeof(DataTypes)).Cast<DataTypes>().ToList();
            List<DataTypes> runProcess = Enum.GetValues(typeof(DataTypes)).Cast<DataTypes>().ToList();

            // Let's pull all data into a MySQL Database
            MySQL database = new MySQL { ConnectionString = "Server=localhost;Database=TIGER;Uid=dbuser;Pwd=test1234$;" };
            
            // Set up our Action Block definition
            var workerBlock = new ActionBlock<DataTypes>(async dataType => 
                {
                    try
                    {
                        Console.WriteLine("***** Starting the Extract of: " + dataType.ToString("g") + " data!");
                        currentlyProcessing.Add(dataType);
                        toProcess.Remove(dataType);

                        var extract = await Extractor.ExtractData(database, dataType, @"c:\temp", 2019, 2500);
                        
                        currentlyProcessing.Remove(dataType);
                        doneProcessing.Add(dataType);
                        Console.WriteLine("***** Completed the Extract of: " + dataType.ToString("g") + " data!");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 8, EnsureOrdered = false });

            // Loop through each of the enum values representing the Tiger data types we can pull
            foreach (DataTypes dt in runProcess)
            {                
                Thread.Sleep(100);
                // Add this to our action block
                workerBlock.Post(dt);
            }

            // Wait until all of the posts queued in the Action Block have been completed
            while (!workerBlock.Completion.IsCompleted)
            {
                // Display a heartbeat every 5 seconds
                Thread.Sleep(5000);
                Console.WriteLine("\n" + DateTime.Now.ToLogFormat() + "[Currently Processing] " + currentlyProcessing.ToStringList() + " [Awaiting Processing] " + toProcess.ToStringList() + "[Process Completed] " + doneProcessing.ToStringList() + "\n");
            }

            Console.WriteLine(DateTime.Now.ToLogFormat() + " COMPLETE!");
        }


        /// <summary>
        /// A Method to Test TPL
        /// </summary>
        private static void ActionBlockTest()
        {
            var workerBlock = new ActionBlock<string>(tn =>
            {
                for (int x = 0; x < 100; x++)
                {
                    Thread.Sleep(1250);
                    Console.WriteLine(tn + ": " + x);
                }

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded, EnsureOrdered = false });

            workerBlock.Post("A");
            workerBlock.Post("B");
            workerBlock.Post("C");
            workerBlock.Post("D");

            while (!workerBlock.Completion.IsCompleted)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Threads awaiting processing: " + workerBlock.InputCount);
            }
        }
    }
}
