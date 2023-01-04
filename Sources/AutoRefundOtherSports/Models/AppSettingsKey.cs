namespace AutoRefundOtherSports.Models;

public class AppSettingsKey
{
    public int RetryInterval { get; set; }

    public int RetryCount { get; set; }

    public int RetryValidate { get; set; }

    public int RetryValidateCount { get; set; }

    public string EnvironmentName { get; set; }

    public List<string> SiteDbServer { get; set; }
}