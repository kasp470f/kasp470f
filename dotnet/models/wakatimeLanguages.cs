// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

public class wakatimeLanguage
{
    public string color { get; set; }
    public string @decimal { get; set; }
    public string digital { get; set; }
    public int hours { get; set; }
    public int minutes { get; set; }
    public string name { get; set; }
    public double percent { get; set; }
    public string text { get; set; }
    public double total_seconds { get; set; }
}

public class wakatimeLanguages
{
    public List<wakatimeLanguage> data { get; set; }
}