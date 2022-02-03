using System;
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
            JObject wakatimeLanguages = await GetData("https://wakatime.com/share/@kasp470f/09fc97af-59ae-4d9d-a09c-25c3e5ab711c.json");
            JObject wakatimeTime = await GetData("https://wakatime.com/share/@kasp470f/364a7155-4732-4077-932f-b403c54cbd9a.json");

            // Remove all details from the README.md file
            int stats = readme.IndexOf("<!-- Stats -->")+1;
            readme.RemoveRange(stats, readme.Count - stats);

            // Build the new data in a markdown structure
            BuildLanguageStatistics(wakatimeLanguages, wakatimeTime, out string buildString);

            // Add the new statistics to the README.md file
            File.WriteAllText("README.md", buildString);
        }

        private static void BuildLanguageStatistics(JObject wakatimeLanguages, JObject wakatimeTime, out string buildString)
        {

            // Get total time in seconds from WakaTime
            double totalTime = wakatimeTime["data"]["grand_total"]["total_seconds"].Value<double>();

            // Instantiate the string builder
            StringBuilder sb = new StringBuilder();

            // Add existing readme to StringBuilder
            foreach (var line in readme)
            {
                sb.AppendLine(line);
            }

            // Add the details
            sb.AppendLine("<details>");
            sb.AppendLine("<summary align=\"center\">Language Statistics</summary>\n<br>");

            //Setup table
            sb.AppendLine("<table align=\"center\">");
            sb.AppendLine("\t<tr>\n\t\t<th>Language</th>\n\t\t<th>Time Spent</th>\n\t\t<th>Percent</th>\n\t</tr>");
            
            // Get the languages from the data
            foreach (var language in wakatimeLanguages["data"])
            {
                double spentOnLanguage = (double.Parse(language["percent"].ToString()) / 100) * totalTime;
                TimeSpan t = TimeSpan.FromSeconds(spentOnLanguage);
                sb.AppendLine($"\t<tr>\n\t\t<td>{language["name"]}</td>\n\t\t<td>{string.Format("{0:D2}h {1:D2}m", t.Hours, t.Minutes)}</td>\n\t\t<td>{language["percent"]}%</td>\n\t</tr>");
            }

            // Close table
            sb.AppendLine("</table>");
            sb.AppendLine($"<p align=\"center\"><sub>Last Updated: {DateTime.Now}</sub></p>");
            sb.AppendLine($"<p align=\"center\"><sub>Data first recorded on 31th. January of 2022</sub></p>");
            sb.AppendLine("</details>");

            // Output the string
            buildString = sb.ToString();
        }

        static HttpClient client = new HttpClient();
        private static async Task<JObject> GetData(string URL)
        {
            string APIRequest = $"{URL}";
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
