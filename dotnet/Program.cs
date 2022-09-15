using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    private static List<Language> languages = new List<Language>();
    static async Task Main(string[] args)
    {
        // Get README.md file
        string readme = File.ReadAllText("markdown_components/description.md");

        // Get data from wakatime.com
        JObject wakatimeLanguages = await GetData("https://wakatime.com/share/@kasp470f/09fc97af-59ae-4d9d-a09c-25c3e5ab711c.json");
        JObject wakatimeTime = await GetData("https://wakatime.com/share/@kasp470f/364a7155-4732-4077-932f-b403c54cbd9a.json");

        // Build the new data in a markdown structure
        BuildLanguageStatisticsBlock(wakatimeLanguages, wakatimeTime, out string statisticBuildString);

        // Add the new statistics to the README.md file
        File.WriteAllText("README.md", readme + statisticBuildString);
    }

    private static void BuildLanguageStatisticsBlock(JObject wakatimeLanguages, JObject wakatimeTime, out string buildString)
    {
        // Get total time in seconds from WakaTime
        double totalTime = wakatimeTime["data"]["grand_total"]["total_seconds"].Value<double>();

        // Instantiate the language component
        string langComponent = File.ReadAllText("markdown_components/language_section.md");

        // Get the languages from the data
        var tempLanguages = new List<Language>();
        foreach (var language in wakatimeLanguages["data"])
        {
            var languageString = language["name"].ToString();
            if (Ignore(languageString))
            {
                var currentLanguage = new Language();
                double spentOnLanguage = (double.Parse(language["percent"].ToString()) / 100) * totalTime;
                currentLanguage.Time = TimeSpan.FromSeconds(spentOnLanguage);
                currentLanguage.Name = language["name"].ToString();
                tempLanguages.Add(currentLanguage);
            }
        }

        string languageStringSection = string.Empty;
        foreach (var language in tempLanguages)
        {
            if(Same(language.Name, out string keyName)) {
                tempLanguages.Single(x => x.Name == keyName).Time += language.Time;
            }
            else {
                languages.Add(language);
                Console.WriteLine(language);
                languageStringSection += language.ToString() + "\n";
            }
        }

        // Build the string
        langComponent = langComponent.Replace("{{LANGUAGE_SECTION}}", languageStringSection);
        langComponent = langComponent.Replace("{{TIME_OF_UPDATE}}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

        buildString = langComponent;
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

    private static bool Ignore(string name)
    {
        var exclude = new List<string> {
                "JSON",
                "Other",
                "XAML",
                "Git Config",
                "YAML",
                "Text",
                "Perl",
                "XML",
                "Objective-C",
                "C",
                "GIT",
                "INI",
                "Image (png)",
                "Batchfile"
            };
        return !exclude.Select(x => x.ToLower()).Contains(name.ToLower());
    }

    private static bool Same(string checkLanguage,out string keyName) {
        keyName = string.Empty;
        var sameLanguage = new Dictionary<string, string>() {
            { "SCSS", "CSS" },
            { "CSHTML", "HTML" }
        };
        return sameLanguage.TryGetValue(checkLanguage, out keyName);
    }
}

class Language {
    public string Name { get; set; }
    public TimeSpan Time { get; set; }


    private string TimeFormat => string.Format("{0} hours {1:D2} minutes", Math.Floor(Time.TotalHours).ToString().PadLeft(2, '0'), Time.Minutes);

    public override string ToString()
    {
        return $"{Name.PadRight(15)} | {TimeFormat}";
    }
}
