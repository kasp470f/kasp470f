﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace dotnet
{
    class Program
    {
        private static List<string> readme;
        static async Task Main(string[] args)
        {
            // Get README.md file
            readme = File.ReadAllLines("README.md").ToList();

            // Get data from wakatime.com
            JObject wakatime = await GetData();

            // Remove all details from the README.md file
            int details = 6;
            readme.RemoveRange(details, readme.Count - details);


            StringBuilder sb = new StringBuilder();

            // Add existing readme to StringBuilder
            foreach (var line in readme)
            {
                sb.AppendLine(line);
            }

            // Add the details
            sb.AppendLine("<details>");
            sb.AppendLine("<summary align=\"center\">Language Statistics</summary>");


            //Setup table
            sb.AppendLine("<table align=\"center\">");
            sb.AppendLine("\t<tr>\n\t\t<th>Language</th>\n\t\t<th>Percent</th>\n\t</tr>");

            // Get the languages from the data
            foreach (var language in wakatime["data"])
            {
                sb.AppendLine($"\t<tr>\n\t\t<td>{language["name"]}</td>\n\t\t<td>{language["percent"]}%</td>\n\t</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine($"<p align=\"center\"><sub>Last Updated: {DateTime.Now}</sub></p>");
            sb.AppendLine("</details>");

            File.WriteAllText("README.md", sb.ToString());
            // System.Console.WriteLine(sb.ToString());
        }

        static HttpClient client = new HttpClient();
        private static async Task<JObject> GetData()
        {
            string APIRequest = $"https://wakatime.com/share/@kasp470f/09fc97af-59ae-4d9d-a09c-25c3e5ab711c.json";
            string text = null;
            HttpResponseMessage response = await client.GetAsync(APIRequest);
            if (response.IsSuccessStatusCode)
            {
                text = await response.Content.ReadAsStringAsync();
            }
            return JObject.Parse(text);
        }
    }
}
