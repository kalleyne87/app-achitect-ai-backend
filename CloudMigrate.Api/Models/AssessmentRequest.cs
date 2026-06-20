namespace CloudMigrate.Api.Models
{
    public class AssessmentRequest
    {
        public string ApplicationName { get; set; } = "";
        public string ApplicationType { get; set; } = "";
        public string TechStack { get; set; } = "";
        public string HostingEnvironment { get; set; } = "";
        public int UserCount { get; set; }
        public string DatabaseType { get; set; } = "";
        public string Requirements { get; set; } = "";
    }
}