using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CensusData.Extract;

using static CensusData.Extract.Enums;

namespace CensusData
{
    class Program
    {
        static void Main(string[] args)
        {
            // Let's pull all data into a MySQL Database
            MySQL database = new MySQL { ConnectionString = "Server=localhost;Database=TIGER;Uid=dbuser;Pwd=test1234$;" };
            
            // Set up our Action Block definition
            var workerBlock = new ActionBlock<DataTypes>(async dataType => 
                {
                    try
                    {
                        var extract = await Extractor.ExtractData(database, dataType, @"c:\temp", 2019, 2500);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 8 });

            // Loop through each of the enum values representing the Tiger data types we can pull
            foreach (DataTypes dt in Enum.GetValues(typeof(DataTypes)).Cast<DataTypes>().ToList())
            {
                Thread.Sleep(500);
                // Add this to our action block
                workerBlock.Post(dt);
            }
                       
            // Wait until all of the posts queued in the Action Block have been completed
            workerBlock.Completion.Wait();           
        }
    }
}
