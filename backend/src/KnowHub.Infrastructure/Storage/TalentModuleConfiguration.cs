namespace KnowHub.Infrastructure.Storage;

public class TalentModuleConfiguration
{
    public const string SectionName = "TalentModule";
    public long MaxFileSizeBytes { get; set; } = 10_485_760; // 10MB
    public int MaxFilesPerJob { get; set; } = 100;
    public int MaxConcurrentScreening { get; set; } = 5;
    public string[] AllowedFileExtensions { get; set; } = [".pdf", ".docx"];
    public string[] AllowedSharePointDomains { get; set; } = [];
}
