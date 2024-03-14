using System.Collections.Generic;

public class wakatimeLanguage
{
    private string color { get; set; }
    private string @decimal { get; set; }
    private string digital { get; set; }
    public string text { get; set; }
    public int hours { get; set; }
    public int minutes { get; set; }
    public string name { get; set; }
    public double percent { get; set; }
    public double total_seconds { get; set; }
}

public class wakatimeLanguages
{
    public List<wakatimeLanguage> data { get; set; }
}