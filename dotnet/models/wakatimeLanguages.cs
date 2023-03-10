// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

public class wakatimeLanguage
{
    public string color { get; set; }
    public string name { get; set; }
    public double percent { get; set; }
}

public class wakatimeLanguages
{
    public List<wakatimeLanguage> data { get; set; }
}