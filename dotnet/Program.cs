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
        static async Task Main(string[] args)
        {
            // Get README.md file
            string readme = File.ReadAllText("markdown_components/description.md");

            // Get data from wakatime.com
            JObject wakatimeLanguages = await GetData("https://wakatime.com/share/@kasp470f/09fc97af-59ae-4d9d-a09c-25c3e5ab711c.json");
            JObject wakatimeTime = await GetData("https://wakatime.com/share/@kasp470f/364a7155-4732-4077-932f-b403c54cbd9a.json");

            // Build the new data in a markdown structure
            // BuildLanguageStatisticsTable(wakatimeLanguages, wakatimeTime, out string statisticBuildString);'
            BuildLanguageStatisticsBlock(wakatimeLanguages, wakatimeTime, out string statisticBuildString);

            // Add the new statistics to the README.md file
            File.WriteAllText("README.md", readme + statisticBuildString);
        }

        private static void BuildLanguageStatisticsTable(JObject wakatimeLanguages, JObject wakatimeTime, out string buildString)
        {
            // Get total time in seconds from WakaTime
            double totalTime = wakatimeTime["data"]["grand_total"]["total_seconds"].Value<double>();

            // Instantiate the string builder
            string sb = string.Empty;

            // Add the details
            sb += "<details>" + "\n";
            sb += "<summary align=\"center\"><strong>Language Statistics</strong></summary>\n<br>" + "\n";

            //Setup table
            sb += "<table align=\"center\">" + "\n";
            sb += "\t<tr>\n\t\t<th>Language</th>\n\t\t<th>Time Spent</th>\n\t</tr>" + "\n";
            
            // Get the languages from the data
            foreach (var language in wakatimeLanguages["data"])
            {
                double spentOnLanguage = (double.Parse(language["percent"].ToString()) / 100) * totalTime;
                TimeSpan t = TimeSpan.FromSeconds(spentOnLanguage);
                sb += $"\t<tr>\n\t\t<td>{language["name"]}</td>\n\t\t<td>{string.Format("{0:D2}h {1:D2}m", t.Hours, t.Minutes)}</td>\n\t</tr>" + "\n";
            }

            // Close table
            sb += "</table>" + "\n";
            sb += $"<p align=\"center\"><sub>Last Updated: {DateTime.Now}</sub></p>" + "\n";
            sb += $"<p align=\"center\"><sub>Data first recorded on 31th. January of 2022</sub></p>" + "\n";
            sb += ("</details>");

            // Output the string
            buildString = sb.ToString();
        }

        private static void BuildLanguageStatisticsBlock(JObject wakatimeLanguages, JObject wakatimeTime, out string buildString)
        {
            // Get total time in seconds from WakaTime
            double totalTime = wakatimeTime["data"]["grand_total"]["total_seconds"].Value<double>();

            // Instantiate the string builder
            string sb = string.Empty;

            // Add the details
            sb += "<details>" + "\n";
            sb += "<summary align=\"center\"><strong>Language Statistics</strong></summary>\n<br>" + "\n";

            // Add center div
            sb += "<div align=\"center\">" + "\n";

            // Add code block
            sb += "<pre>" + "\n";
            
            // Get the languages from the data
            foreach (var language in wakatimeLanguages["data"])
            {
                double spentOnLanguage = (double.Parse(language["percent"].ToString()) / 100) * totalTime;
                TimeSpan t = TimeSpan.FromSeconds(spentOnLanguage);
                sb += $"{language["name"].ToString().PadRight(15)}| {string.Format("{0:D2} hours {1:D2} minutes", t.Hours, t.Minutes)}" + "\n";
            }
            
            // Get time
            sb += $"<sub>Last Updated: {DateTime.Now}</sub>" + "\n";
            sb += $"<sub>Data first recorded on 31th. January of 2022</sub>" + "\n";

            // Close code block and center div
            sb += "</pre>" + "\n";
            sb += "</div>" + "\n";
            sb += "</details>";

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
