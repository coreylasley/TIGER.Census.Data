# TIGER.Census.Data
Extract All TIGER Data from census.gov into MySQL, SQL Server, PostgreSQL. Currently in very early stages of development, but functioning under a MySQL implementation.

## CensusData.Extract ##
.NET Core 2.2 Console Application

Extracts All TIGER Data from census.gov which can be used for all sorts of GIS operations. 
Only an empty database is required, as the app automatically generates the schema as it extracts data.
The application is multithreaded, and can pick up where it left off if previously terminated.

Dependencies:
DbfDataReader (0.5.2)
HtmlAgilityPack (1.11.17)
MySql.Data (8.0.18)
System.IO.Compression.ZipFile (4.3.0)
System.Threading.Tasks.Dataflow (4.11.0)
