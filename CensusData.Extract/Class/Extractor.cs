using DbfDataReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tiger.Entities;
using Tiger.Interface;
using static Tiger.Helper.Enums;
using Tiger.Helper;

namespace Tiger
{
    public class Extractor
    {
        /// <summary>
        /// Extracts data from census.gov and imports it into a database
        /// </summary>
        /// <param name="toDatabase">The ISQL implementation representing the database to import the data</param>
        /// <param name="dataType">The Data Type to be extracted from census.gov and imported</param>
        /// <param name="downloadPath">The location where the ZIP files should be downloaded to</param>
        /// <param name="year">The census year data to download</param>
        /// <param name="recordsPerBatch">The number of records to insert in a single batch</param>
        /// <returns>Total imported record count</returns>
        public static async Task<DetailedReturn> ExtractData(ISQL toDatabase, DataTypes dataType, string downloadPath, int year, int recordsPerBatch = 1500)
        {
            DetailedReturn ret = new DetailedReturn();
            
            string unzipFolder = @"\unzipTemp\" + Guid.NewGuid() + @"\";

            await Task.Run(async () =>
            {
                
                Downloader d = new Downloader();
                List<string> failedDowloads = new List<string>();

                toDatabase.CreateImportDetailTable();

                // Get all links from the corresponding data page on census.gov
                List<string> addr = await d.GetLinks(year, dataType);

                int filecount = addr.Count();
                int curfile = 0;

                // Get a list of IMPORTDETAIL record that indicate the corresponding file has already completed its import
                List<ImportDetail> alreadyDone = toDatabase.GetCompletedImportDetailList(dataType);

                if (addr.Count == alreadyDone.Count)
                {
                    ret.TotalRecordsAlreadyInDB = alreadyDone.Select(x => x.LastRecordNum).Sum();
                }
                else // Only proceed if we have not yet completed the import for this data type...
                {
                    // Loop through each of the links 
                    foreach (string link in addr)
                    {
                        curfile++;
                        
                        // Proceed only if this link is not contained in the list of completed files
                        if (!alreadyDone.Where(x => x.URL == link).Any())
                        {
                            string outHeader = "[" + dataType.ToString("g") + ": " + curfile + "/" + filecount + "]: ";
                            Console.WriteLine(DateTime.Now.ToLogFormat() + outHeader + link);

                            // Attempt to download the file 
                            string dl = await d.DownloadFile(link, downloadPath);

                            // Only proceed if the file was successfully downloaded...
                            if (dl != "")
                            {
                                // Attempt to unzip the downloaded file, and get the name of the "DBF" file from the extract
                                string fileToImport = d.Unzip(dl, downloadPath + unzipFolder).Where(x => x.ToLower().Contains(".dbf")).FirstOrDefault();
                                
                                // Only proceed if we have a DBF file to process...
                                if (fileToImport != "")
                                {
                                    // Attempt to process the file
                                    DetailedReturn thisFile = ImportTigerData(toDatabase, dataType, fileToImport, recordsPerBatch, link, dl);
                                    ret.TotalRecordsImported += thisFile.TotalRecordsImported;
                                    ret.TotalRecordsAlreadyInDB += thisFile.TotalRecordsAlreadyInDB;
                                    ret.TotalRecordsInFile += thisFile.TotalRecordsInFile;
                                    ret.Errors.AddRange(thisFile.Errors);
                                    
                                    Console.WriteLine(DateTime.Now.ToLogFormat() + outHeader + thisFile.TotalRecordsImported + " records imported this session. Total records in DB now at: " + ret);
                                    
                                    // Remove the extracted files and corresponding temporary directory
                                    d.CleanFolder(downloadPath + unzipFolder);
                                }
                                else
                                {
                                    toDatabase.InsertImportDetails(link, "BAD FILE", dataType);                                    
                                    ret.Errors.Add(new ErrorDetail(ErrorTypes.BadOrMissingLocalFile, dataType, "No DB file found to import", link, ""));
                                }
                            }
                            else
                            {
                                toDatabase.InsertImportDetails(link, "UNABLE TO DOWNLOAD", dataType);
                                failedDowloads.Add(link);

                                ret.Errors.Add(new ErrorDetail(ErrorTypes.CouldNotDownloadFile, dataType, "Failed to download", link, ""));
                            }
                        }

                    }
                }
            });

            return ret;
        }


        private static DetailedReturn ImportTigerData(ISQL toDatabase, DataTypes dataType, string fileToImport, int recordsPerBatch, string referenceURL, string referenceZipFile)
        {
            DetailedReturn ret = new DetailedReturn();
                        
            // CREATE the IMPORTDETAIL table in the Database if it doesnt already exist
            toDatabase.CreateImportDetailTable();

            // Make sure the file exists that we wish to import...
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

                    // Get the IMPORTDETAIL record that matches the URL/File we are going to import
                    ImportDetail importDetail = toDatabase.GetImportDetail(referenceURL);
                    if (importDetail == null)
                    {
                        // If no matching IMPORTDETAIL file was found, create one
                        toDatabase.InsertImportDetails(referenceURL, referenceZipFile, dataType);
                        importDetail = toDatabase.GetImportDetail(referenceURL); //new ImportDetail { URL = referenceURL, LocalFile = referenceZipFile, FileType = dataType.ToString("g"), LastRecordNum = 0 };
                    }

                    ret.TotalRecordsAlreadyInDB = importDetail.LastRecordNum;

                    // Proceed only if the record count in the file exceeds that which we have already imported
                    if (recordCount > importDetail.LastRecordNum)
                    {

                        // The DataTypes enum name is going to be our TABLE name
                        string tableName = dataType.ToString("g");

                        // Generate a CREATE TABLE script representing the DbfTable
                        string tableCreate = toDatabase.GetCreateTableScript(dbfTable, tableName);
                        Debug.WriteLine(tableCreate);

                        // CREATE IF NOT EXISTS our TABLE in the DB
                        toDatabase.ExecuteNonQuery(tableCreate);
                        
                        // Create the first part of our INSERT statement
                        string insertHeader = toDatabase.GetInsertHeader(dbfTable, tableName);

                        // Get the records from the DbfTable
                        var dbfRecord = new DbfRecord(dbfTable);

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
                                    StringBuilder rowPart = new StringBuilder();

                                    // Loop through each of the values in our record
                                    foreach (var dbfValue in dbfRecord.Values)
                                    {
                                        col++;
                                        // Get the column type of this value
                                        DbfColumn c = dbfTable.Columns[col];
                                        // Format the value properly for our INSERT statment
                                        rowPart.Append(toDatabase.FormatValueForInsert(c.ColumnType, dbfValue.ToString()) + ",");
                                    }
                                    // Add the Import Detail ID for our last column value
                                    rowPart.Append(importDetail.ID);

                                    // Append the record values to our INSERT statement
                                    multiInsert.Append("(" + rowPart.ToString() + "),");

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
                                                                                
                                    }
                                }

                                // Update the record count
                                ret.TotalRecordsImported++;
                            }
                            catch (Exception ex)
                            {
                                //Debug.WriteLine(ex.Message);
                                ret.Errors.Add(new ErrorDetail(ErrorTypes.Exception, dataType, ex.Message, referenceURL, fileToImport));
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
                        toDatabase.UpdateImportDetails(referenceURL, recordCount, true);
                    }

                }
            }
            else // If the file does not exist
            {                
                ret.Errors.Add(new ErrorDetail(ErrorTypes.BadOrMissingLocalFile, dataType, "File not found", referenceURL, fileToImport));
            }

            return ret;
        }
    }
}
