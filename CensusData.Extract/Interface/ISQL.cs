using DbfDataReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace CensusData.Extract
{
    public interface ISQL
    {
        bool CreateImportDetailTable();
        string GetCreateTableScript(DbfTable tbl, string tableName);
        bool UpdateImportDetails(string URL, long lastRecordNum, bool isImportComplete);
        bool InsertImportDetails(string URL, string localFile, Enums.DataTypes dataType);
        int ExecuteCount(string sql);
        ImportDetail GetImportDetail(string url);
        List<ImportDetail> GetCompletedImportDetailList(Enums.DataTypes dataType);
        int ExecuteNonQuery(string sql);
        string GetColumnDefinition(DbfColumnType colType, int typeLength, string colName);
        string FormatValueForInsert(DbfColumnType colType, string value);

    }
}
