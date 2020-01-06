using DbfDataReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace CensusData.Extract
{
    public interface ISQL
    {
        /// <summary>
        /// Creates the ImportDetail table in the database if it does not already exist
        /// </summary>
        /// <returns>boolean indicating success</returns>
        bool CreateImportDetailTable();

        /// <summary>
        /// Generates a CREATE TABLE script representing the DbfTable and a specified table name
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetCreateTableScript(DbfTable tbl, string tableName);

        /// <summary>
        /// Updates the related ImportDetail record (based on URL) in the Database
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="lastRecordNum"></param>
        /// <param name="isImportComplete"></param>
        /// <returns>boolean indicating success</returns>
        bool UpdateImportDetails(string URL, long lastRecordNum, bool isImportComplete);

        /// <summary>
        /// Inserts a new ImportDetail record in the Database
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="localFile"></param>
        /// <param name="dataType"></param>
        /// <returns>boolean indicating success</returns>
        bool InsertImportDetails(string URL, string localFile, Enums.DataTypes dataType);

        /// <summary>
        /// Returns a total record count for a specified SQL command
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        int ExecuteCount(string sql);

        /// <summary>
        /// Returns an ImportDetail object representing an IMPORTDETAIL record in the database that matches on the URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        ImportDetail GetImportDetail(string url);
        
        /// <summary>
        /// Returns a List of ImportDetail objects representing IMPORTDETAIL records from the database for the specified DataType that is marked as Completed
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns>List of ImportDetail objects</returns>
        List<ImportDetail> GetCompletedImportDetailList(Enums.DataTypes dataType);

        /// <summary>
        /// Executes a non-query command against the database, and returns the number of records impacted
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        int ExecuteNonQuery(string sql);

        /// <summary>
        /// Generates the INSERT command for the database relating to the DbfTable and a table name
        /// </summary>
        /// <param name="dbfTable"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetInsertHeader(DbfTable dbfTable, string tableName);

        /// <summary>
        /// Generates a COLUMN definition to be used in a CREATE TABLE command
        /// </summary>
        /// <param name="colType"></param>
        /// <param name="typeLength"></param>
        /// <param name="colName"></param>
        /// <returns></returns>
        string GetColumnDefinition(DbfColumnType colType, int typeLength, string colName);

        /// <summary>
        /// Formats a value string for an INSERT/UPDATE statement based on the DbfColumnType
        /// </summary>
        /// <param name="colType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        string FormatValueForInsert(DbfColumnType colType, string value);

    }
}
