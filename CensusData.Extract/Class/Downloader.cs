using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Tiger.Helper.Enums;

namespace Tiger
{
    public class Downloader
    {
       
        /// <summary>
        /// Returns a List of URL's representing the ZIP links on the Census site for the given dataType
        /// </summary>
        /// <param name="year">The Census year to pull</param>
        /// <param name="dataType">the Data Type of the files to pull</param>
        /// <returns></returns>
        public async Task<List<string>> GetLinks(int year, DataTypes dataType)
        {
            List<string> ret = new List<string>();

            await Task.Run(() =>
            {
                string url = BaseURL + BaseYearPath + year + "/" + dataType.ToString("g");

                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(url);
                try
                {
                    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        if (att.Value.ToLower().EndsWith(".zip")) ret.Add(url + @"/" + att.Value);
                    }
                }
                catch
                {
                    Debug.WriteLine("***** FAILED TO PARSE: " + url); 
                }
            });

            return ret;
        }

        /// <summary>
        /// Downloads the file specified in the url to the saveToFolder location
        /// </summary>
        /// <param name="url">the URL of the file to download</param>
        /// <param name="saveToFolder">The folder to save the file locally</param>
        /// <returns></returns>
        public async Task<string> DownloadFile(string url, string saveToFolder, int retryAttempts = 3)
        {
            string ret = "";

            await Task.Run(() =>
            {
                try
                {
                    string filename = "";
                    Uri uri = new Uri(url);
                    
                    filename = Path.GetFileName(uri.LocalPath);

                    if (filename != "")
                    {
                        ret = (saveToFolder + @"\" + filename).Replace(@"\\", @"\");
                        bool retry = true;
                        if (!File.Exists(ret))
                        {
                            while (retry)
                            {
                                var wc = new WebClient();
                                wc.DownloadFile(url, ret);

                                if (!File.Exists(ret))
                                {
                                    retryAttempts--;
                                    if (retryAttempts == 0)
                                    {
                                        ret = "";
                                        retry = false;
                                    }
                                }
                                else
                                {
                                    retry = false;
                                }
                            }
                        }

                    }
                }
                catch { }
            });

            return ret;
        }

        public List<string> Unzip(string zipPath, string extractPath)
        {
            List<string> ret = new List<string>();
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                ret = Directory.GetFiles(extractPath).OfType<string>().ToList();
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Deletes all files in a directory 
        /// </summary>
        /// <param name="extractPath"></param>
        /// <returns></returns>
        public int CleanFolder(string extractPath)
        {
            int ret = 0;
            if (Directory.Exists(extractPath))
            {
                foreach (string f in Directory.GetFiles(extractPath))
                {
                    try
                    {
                        File.Delete(f);
                        ret++;
                    }
                    catch { }
                }

                try
                {
                    Directory.Delete(extractPath);
                }
                catch { }
            }

            return ret;
        }
               
    }
}
