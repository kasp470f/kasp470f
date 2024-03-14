using System;

public class Language {
    public string Name { get; set; }
    public TimeSpan Time { get; set; }

    private string TimeFormat => string.Format("{0} hours {1:D2} minutes", Math.Floor(Time.TotalHours).ToString().PadLeft(2, '0'), Time.Minutes);

    public override string ToString()
    {
        return $"{Name.PadRight(15)} | {TimeFormat.PadLeft(21)}";
    }
}
