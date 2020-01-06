using System.Text;
using System;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using DbfDataReader;
using static CensusData.Extract.Enums;
using System.Collections.Generic;

namespace CensusData.Extract
{

    /// <summary>
    /// MySQL implementation of ISQL
    /// </summary>
    public class MySQL : DbfReader, ISQL
    {
            
        public string ConnectionString { get; set; }

        /// <summary>
        /// Creates the IMPORTDETAIL table in the DB, which is used to keep track of import progress
        /// </summary>
        /// <returns></returns>
        public bool CreateImportDetailTable()
        {
            bool ret = true;

            int tblCreated = ExecuteNonQuery("CREATE TABLE IF NOT EXISTS IMPORTDETAIL (ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY, FILETYPE VARCHAR(15),URL VARCHAR(250),LOCALFILE VARCHAR(500),LASTRECORDNUM INT)");
            if (tblCreated >= 0)
            {
                int index = ExecuteCount("SHOW INDEX FROM IMPORTDETAIL WHERE Key_name = 'IDX_URL'");
                if (index == 0)
                {
                    ExecuteNonQuery("CREATE INDEX IDX_URL ON IMPORTDETAIL (URL)");
                }
            }
            else
                return false;

            return ret;
        }

        

        public string GetCreateTableScript(DbfTable tbl, string tableName)
        {
            string sql = "CREATE TABLE IF NOT EXISTS " + tableName + " (\n";
            string cols = "";
            foreach (var dbfColumn in tbl.Columns)
            {
                if (cols != "") cols += ",\n";
                cols += GetColumnDefinition(dbfColumn.ColumnType, dbfColumn.Length, dbfColumn.Name);
            }
            cols += ", IMPORTDETAILID INT";

            sql += cols + "\n)";

            return sql;
        }

        public bool UpdateImportDetails(string URL, long lastRecordNum, bool isImportComplete)
        {           
            return ExecuteNonQuery("UPDATE IMPORTDETAIL SET LASTRECORDNUM = " + lastRecordNum + ", ISCOMPLETE = " + (isImportComplete ? "TRUE" : "FALSE") + " WHERE URL = '" + URL + "'") > 0 ? true : false;
        }

        public bool InsertImportDetails(string URL, string localFile, DataTypes dataType)
        {
            return ExecuteNonQuery("INSERT IMPORTDETAIL SET ISCOMPLETE = FALSE, LASTRECORDNUM = 0, URL = '" + URL + "', FILETYPE = '" + dataType.ToString("g") + "', LOCALFILE = '" + localFile.Replace(@"\", @"\\") + "'") > 0 ? true : false;
        }

       

        /// <summary>
        /// Executes a DB query and returns the number of records returned in the result
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int ExecuteCount(string sql)
        {
            int ret = 0;

            try
            {
                MySqlConnection dbCon = new MySqlConnection(ConnectionString);
                dbCon.Open();

                MySqlCommand dbCmd = new MySqlCommand(sql, dbCon);
                var r = dbCmd.ExecuteReader();
                while(r.Read())
                {
                    ret++;
                }
                dbCon.Close();                                
            }
            catch {
                ret = -1;
            }

            return ret;
        }

        public ImportDetail GetImportDetail(string url)
        {
            ImportDetail ret = null;

            try
            {
                MySqlConnection dbCon = new MySqlConnection(ConnectionString);
                dbCon.Open();

                MySqlCommand dbCmd = new MySqlCommand("SELECT FILETYPE, LOCALFILE, LASTRECORDNUM, ID, ISCOMPLETE FROM IMPORTDETAIL WHERE URL = '" + url + "'", dbCon);
                var r = dbCmd.ExecuteReader();
                while (r.Read())
                {
                    ret = new ImportDetail();
                    ret.URL = url;
                    ret.FileType = r.GetValue(0).ToString();
                    ret.LocalFile = r.GetValue(1).ToString();
                    ret.LastRecordNum = Convert.ToInt32(r.GetValue(2));
                    ret.ID = Convert.ToInt32(r.GetValue(3));
                    ret.IsComplete = Convert.ToBoolean(r.GetValue(4));
                    break;
                }
                dbCon.Close();
            }
            catch
            {
                ret = null;
            }

            return ret;
        }

        public List<ImportDetail> GetCompletedImportDetailList(Enums.DataTypes dataType)
        {
            List<ImportDetail> ret = new List<ImportDetail>();

            try
            {
                MySqlConnection dbCon = new MySqlConnection(ConnectionString);
                dbCon.Open();

                string sql = "SELECT FILETYPE, LOCALFILE, LASTRECORDNUM, ID, URL FROM IMPORTDETAIL WHERE ISCOMPLETE = TRUE AND FILETYPE = '" + dataType.ToString("g") + "'";
                MySqlCommand dbCmd = new MySqlCommand(sql, dbCon);
                var r = dbCmd.ExecuteReader();
                while (r.Read())
                {
                    ImportDetail id = new ImportDetail();                    
                    id.FileType = r.GetValue(0).ToString();
                    id.LocalFile = r.GetValue(1).ToString();
                    id.LastRecordNum = Convert.ToInt32(r.GetValue(2));
                    id.ID = Convert.ToInt32(r.GetValue(3));
                    id.URL = r.GetValue(4).ToString();
                    ret.Add(id);                    
                }
                dbCon.Close();
            }
            catch
            {
                ret = null;
            }

            return ret;
        }

        /// <summary>
        /// Executes an INSERT, UPDATE, DELETE command on the DB
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql)
        {
            int ret = 0;
            try
            {                
                MySqlConnection dbCon = new MySqlConnection(ConnectionString);
                dbCon.Open();

                MySqlCommand dbCmd = new MySqlCommand(sql, dbCon);
                ret = dbCmd.ExecuteNonQuery();
                dbCon.Close();                                
            }
            catch(Exception ex) {
                Debug.WriteLine(ex.Message);
            }

            return ret;
        }

        public string GetInsertHeader(DbfTable dbfTable, string tableName)
        {
            // Create a string representing all the columns in the TABLE for our INSERT statement
            string columnPart = "";
            foreach (var dbfColumn in dbfTable.Columns)
            {
                columnPart += "`" + dbfColumn.Name + "`,";
            }
            columnPart += "`IMPORTDETAILID`";

            // Create the first part of our INSERT statement
            return "INSERT INTO " + tableName + " (" + columnPart + ") VALUES ";
        }

        /// <summary>
        /// Creates a COLUMN definition for the given column that can be used in a CREATE TABLE script
        /// </summary>
        /// <param name="colType">The DBF Column Type to be mapped</param>
        /// <param name="typeLength">The defined Length of the Column</param>
        /// <param name="colName">The Name of the Column</param>
        /// <returns></returns>
        public string GetColumnDefinition(DbfColumnType colType, int typeLength, string colName)
        {
            string dt = "";
            switch (colType)
            {
                case DbfColumnType.Boolean:
                    dt = "BIT";
                    break;
                case DbfColumnType.Date:
                    dt = "DATE";
                    break;
                case DbfColumnType.DateTime:
                    dt = "DATETIME";
                    break;
                case DbfColumnType.Currency:
                case DbfColumnType.Double:
                case DbfColumnType.Float:
                case DbfColumnType.Number:
                case DbfColumnType.Signedlong:
                    dt = "DECIMAL";
                    break;
                default:
                    dt = "VARCHAR";
                    break;
            }

            return "`" + colName + "` " + dt + (typeLength > 0 ? "(" + typeLength + ")" : "");
        }

        /// <summary>
        /// Formats a value for inclusion in an INSERT statement
        /// </summary>
        /// <param name="colType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string FormatValueForInsert(DbfColumnType colType, string value)
        {
            string ret = "";
            
            switch(colType)
            {
                case DbfColumnType.Boolean:
                    ret = (Convert.ToBoolean(value) == true ? "1" : "0");
                    break;
                case DbfColumnType.Date:
                case DbfColumnType.DateTime:
                    ret = "'" + Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                    break;
                case DbfColumnType.Currency:
                case DbfColumnType.Double:
                case DbfColumnType.Float:
                case DbfColumnType.Number:
                case DbfColumnType.Signedlong:
                    if (value == "") 
                        value = "NULL";
                    ret = value.Replace("$","");
                    break;
                default:
                    ret = "'" + value.Replace("'","''") + "'";
                    break;
            }

            return ret;
        }       

       
    }
}
