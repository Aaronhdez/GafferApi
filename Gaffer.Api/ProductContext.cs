namespace Gaffer.Api;

public class ProductContext {
    public string Name { get; set; } = "Gaffer API";
    public string Id { get; set; } = "gaffer-api";
    public string Environment { get; set; } = "Development";
    public string BootLoggingFile { get; set; } = "startup.json";
    public string StartupSettingsFileName { get; set; } = "startup-settings.log";
}