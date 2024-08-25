using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private static List<Language> languages = new List<Language>();
    static async Task Main(string[] args)
    {
        // Get README.md file
        string readme = File.ReadAllText("markdown_components/description.md");

        wakatimeLanguages languages = JsonConvert.DeserializeObject<wakatimeLanguages>(await GetData("https://wakatime.com/share/@kasp470f/7017a95a-e41f-4bf2-ac00-4c839e09e549.json"));

        // Build the new data in a markdown structure
        string statisticBuildString = BuildLanguageStatisticsBlock(languages);

        if(statisticBuildString != null) {
            // Add the new statistics to the README.md file
            File.WriteAllText("README.md", readme + statisticBuildString);
        }
    }

    private static string BuildLanguageStatisticsBlock(wakatimeLanguages _wakatimeLanguages)
    {
        try
        {
            // Instantiate the language component
            string langComponent = File.ReadAllText(@"markdown_components/language_section.md");

            // Get the languages from the data
            var dataList = _wakatimeLanguages.data;
            var tempLanguages = new List<Language>();
            foreach (var language in dataList)
            {
                var languageString = language.name;
                if (Ignore(languageString))
                {
                    var currentLanguage = new Language
                    {
                        Name = language.name.ToString(),
                        Time = TimeSpan.FromSeconds(language.total_seconds)
                    };
                    if (currentLanguage.Time.Minutes == 0 && currentLanguage.Time.Hours == 0 || currentLanguage.Time.TotalMinutes < 30) continue;
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
                }
            }

            languages.Sort((x, y) => y.Time.CompareTo(x.Time));
            languageStringSection = string.Join('\n', languages);
            languages.ForEach(x => Console.WriteLine(x));


            // Build the string
            langComponent = langComponent.Replace("{{LANGUAGE_SECTION}}", languageStringSection);
            langComponent = langComponent.Replace("{{TIME_OF_UPDATE}}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return langComponent;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(ex);

            Console.ForegroundColor = ConsoleColor.White;

            return null;
        }
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
                "Batchfile",
                "Markdown",
                "SVG",
                "SWIG",
                "Prolog",
                "TSConfig",
                "Ezhil",
                "Assembly",
                "Bash",
                "GDScript",
                "ca65 assembler",
                "ActionScript 3",
                "TSQL"
            };
        return !exclude.Select(x => x.ToLower()).Contains(name.ToLower());
    }

    private static bool Same(string checkLanguage, out string keyName) {
        keyName = string.Empty;
        var sameLanguage = new Dictionary<string, string>() {
            { "SCSS", "CSS" },
            { "CSHTML", "HTML" }
        };
        return sameLanguage.TryGetValue(checkLanguage, out keyName);
    }

    static HttpClient client = new HttpClient();
    private static async Task<string> GetData(string URL)
    {
        string APIRequest = $"{URL}";
        string text = null;
        HttpResponseMessage response = await client.GetAsync(APIRequest);
        if (response.IsSuccessStatusCode)
        {
            text = await response.Content.ReadAsStringAsync();
        } else {
            Console.WriteLine("Error: " + response.StatusCode);
        }
        return text;
    }
}