namespace AutoRefundOtherSports.Models
{
    public class AppSettingsKey
    {
        public int RetryInterval { get; set; }

        public int RetryCount { get; set; }

        public string EnvironmentName { get; set; }

        public string DeploymentDelay { get; set; }

        public string DeploymentSpName { get; set; }
    }
}