using DbfDataReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace CensusData.Extract
{
    public class Extractor
    {

        public static async Task<long> ExtractData(ISQL toDatabase, Enums.DataTypes dataType, string downloadPath, int year, int recordsPerBatch = 1500)
        {
            
            Console.WriteLine("***** Starting the Extract of: " + dataType.ToString("g") + " data!");

            long total = 0;
            
            string unzipFolder = @"\unzipTemp\" + Guid.NewGuid() + @"\";

            await Task.Run( async () =>
            {
                List<ImportDetail> alreadyDone = toDatabase.GetCompletedImportDetailList(dataType);

                Downloader d = new Downloader();
                List<string> failedDowloads = new List<string>();

                toDatabase.CreateImportDetailTable();

                var addr = await d.GetLinks(year, dataType);

                int filecount = addr.Count();
                int curfile = 0;

                if (addr.Count == alreadyDone.Count)
                {
                    total = alreadyDone.Select(x => x.LastRecordNum).Sum();
                }
                else
                {

                    foreach (string link in addr)
                    {
                        curfile++;
                        string outHeader = "[" + dataType.ToString("g") + ": " + curfile + "/" + filecount + "]: ";

                        Console.WriteLine(outHeader + link);

                        if (!alreadyDone.Where(x => x.URL == link).Any())
                        {
                            string dl = d.DownloadFile(link, downloadPath).Result;

                            if (dl != "")
                            {
                                string fileToImport = d.Unzip(dl, downloadPath + unzipFolder).Where(x => x.ToLower().Contains(".dbf")).FirstOrDefault();
                                if (fileToImport != "")
                                {
                                    DetailedReturn thisFile = ImportTigerData(toDatabase, dataType, fileToImport, recordsPerBatch, link, dl);
                                    total += thisFile.TotalRecordsImported + thisFile.TotalRecordsAlreadyInDB;
                                    Console.WriteLine(outHeader + thisFile.TotalRecordsImported + " records imported this session. Total records in DB now at: " + total);
                                    d.CleanFolder(downloadPath + unzipFolder);
                                }
                                else
                                {
                                    toDatabase.InsertImportDetails(link, "BAD FILE", dataType);
                                    Console.WriteLine("No DB file found to import");
                                }
                            }
                            else
                            {
                                toDatabase.InsertImportDetails(link, "UNABLE TO DOWNLOAD", dataType);
                                failedDowloads.Add(link);
                                Console.WriteLine("!!!!!! FILE FAILED TO DOWNLOAD: " + link);
                            }
                        }

                    }
                }
            });

            return total;
        }


        private static DetailedReturn ImportTigerData(ISQL toDatabase, Enums.DataTypes dataType, string fileToImport, int recordsPerBatch, string referenceURL, string referenceZipFile)
        {
            DetailedReturn ret = new DetailedReturn();
            
            Downloader d = new Downloader();

            // CREATE the IMPORTDETAIL table in the Database if it doesnt already exist
            toDatabase.CreateImportDetailTable();

            if (File.Exists(fileToImport))
            {

                // Read the dbfFile contents into a DbfTable object
                using (var dbfTable = new DbfTable(fileToImport, Encoding.UTF8))
                {
                    var header = dbfTable.Header;
                    var versionDescription = header.VersionDescription;
                    var hasMemo = dbfTable.Memo != null;
                    var recordCount = header.RecordCount;
                    int rowsAffected = 0;

                    ret.TotalRecordsInFile = recordCount;

                    ImportDetail importDetail = toDatabase.GetImportDetail(referenceURL);
                    if (importDetail == null)
                    {
                        toDatabase.InsertImportDetails(referenceURL, referenceZipFile, dataType);
                        importDetail = toDatabase.GetImportDetail(referenceURL); //new ImportDetail { URL = referenceURL, LocalFile = referenceZipFile, FileType = dataType.ToString("g"), LastRecordNum = 0 };
                    }

                    ret.TotalRecordsAlreadyInDB = importDetail.LastRecordNum;

                    if (recordCount > importDetail.LastRecordNum)
                    {

                        // The DataTypes enum name is going to be our TABLE name
                        string tableName = dataType.ToString("g");

                        // Generate a CREATE TABLE script representing the DbfTable
                        string tableCreate = toDatabase.GetCreateTableScript(dbfTable, tableName);
                        Debug.WriteLine(tableCreate);

                        // CREATE IF NOT EXISTS our TABLE in the DB
                        toDatabase.ExecuteNonQuery(tableCreate);

                        // Create a string representing all the columns in the TABLE for our INSERT statement
                        string columnPart = "";
                        foreach (var dbfColumn in dbfTable.Columns)
                        {
                            columnPart += "`" + dbfColumn.Name + "`,";
                        }
                        columnPart += "`IMPORTDETAILID`";

                        // Get the records from the DbfTable
                        var dbfRecord = new DbfRecord(dbfTable);

                        // Create the first part of our INSERT statement
                        string insertHeader = "INSERT INTO " + tableName + " (" + columnPart + ") VALUES ";

                        // We are going to INSERT multiple records with each DB Command to dramatically speed things up
                        StringBuilder multiInsert = new StringBuilder();
                        multiInsert.Append(insertHeader);

                        // Loop through each of our records...
                        while (dbfTable.Read(dbfRecord))
                        {
                            try
                            {
                                // We only want to start INSERTing records where we last left off
                                if (ret.TotalRecordsImported >= importDetail.LastRecordNum)
                                {
                                    // Skip the record if it is marked as deleted
                                    if (dbfRecord.IsDeleted)
                                        continue;

                                    int col = -1;
                                    string rowPart = "";

                                    // Loop through each of the values in our record
                                    foreach (var dbfValue in dbfRecord.Values)
                                    {
                                        col++;
                                        // Get the column type of this value
                                        DbfColumn c = dbfTable.Columns[col];
                                        // Format the value properly for our INSERT statment
                                        rowPart += toDatabase.FormatValueForInsert(c.ColumnType, dbfValue.ToString()) + ",";
                                    }
                                    // Add the Import Detail ID for our last column value
                                    rowPart += importDetail.ID;

                                    // Append the record values to our INSERT statement
                                    multiInsert.Append("(" + rowPart + "),");

                                    // If we have collected xxx records...
                                    if (ret.TotalRecordsImported % recordsPerBatch == 0)
                                    {
                                        // Its time to execute the INSERT
                                        rowsAffected = toDatabase.ExecuteNonQuery(multiInsert.ToString().TrimEnd(','));

                                        // If the INSERT was successful 
                                        if (rowsAffected > 0)
                                        {
                                            // UPDATE the IMPORTDETAILS table
                                            toDatabase.UpdateImportDetails(referenceURL, ret.TotalRecordsImported, ret.TotalRecordsImported == recordCount);
                                        }

                                        // Prepare the next mass INSERT statement
                                        multiInsert.Clear();
                                        multiInsert.Append(insertHeader);

                                        //Debug.WriteLine(ret);
                                    }
                                }

                                // Update the record count
                                ret.TotalRecordsImported++;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }

                        ret.TotalRecordsImported -= importDetail.LastRecordNum;

                        // If we have a remaining INSERT compiled....
                        if (multiInsert.ToString() != "" && multiInsert.ToString() != insertHeader)
                        {
                            // execute the INSERT
                            rowsAffected = toDatabase.ExecuteNonQuery(multiInsert.ToString().TrimEnd(','));

                            // If the INSERT was successful
                            if (rowsAffected > 0)
                            {
                                // UPDATE the IMPORTDETAILS table
                                toDatabase.UpdateImportDetails(referenceURL, ret.TotalRecordsImported, ret.TotalRecordsImported == recordCount);
                            }
                        }
                    }
                    else // If we already appeared to have imported all the records in this file
                    {
                        toDatabase.UpdateImportDetails(referenceURL, Convert.ToInt32(recordCount), true);
                    }

                }
            }
            else // If the file does not exist
            {

            }

            return ret;
        }
    }

    public class DetailedReturn
    {
        public long TotalRecordsInFile { get; set; }
        public long TotalRecordsAlreadyInDB { get; set; }
        public long TotalRecordsImported { get; set; }
        public bool CompletedSuccessfully { get; set; }
        public List<string> Errors { get; set; }

        public DetailedReturn()
        {
            Errors = new List<string>();
        }
    }
}
