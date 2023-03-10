using System;

public class BestDay
{
    public string date { get; set; }
    public string text { get; set; }
    public double total_seconds { get; set; }
}

public class wakatimeCodeActivityData
{
    public BestDay best_day { get; set; }
    public GrandTotal grand_total { get; set; }
    public Range range { get; set; }
}

public class GrandTotal
{
    public double daily_average { get; set; }
    public double daily_average_including_other_language { get; set; }
    public string human_readable_daily_average { get; set; }
    public string human_readable_daily_average_including_other_language { get; set; }
    public string human_readable_total { get; set; }
    public string human_readable_total_including_other_language { get; set; }
    public double total_seconds { get; set; }
    public double total_seconds_including_other_language { get; set; }
}

public class Range
{
    public int days_including_holidays { get; set; }
    public int days_minus_holidays { get; set; }
    public DateTime end { get; set; }
    public int holidays { get; set; }
    public string range { get; set; }
    public DateTime start { get; set; }
}

public class wakatimeCodeActivity
{
    public wakatimeCodeActivityData data { get; set; }
}

