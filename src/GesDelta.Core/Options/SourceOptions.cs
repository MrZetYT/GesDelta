namespace GesDelta.Core.Options;

public class SourcesOptions
{
    public string UserAgent { get; set; } = "";
    public int HostDelayMs { get; set; } = 1000;
    public Dictionary<string, int> HostDelaysMs { get; set; } = new();
    public int RssFullTextMinChars { get; set; } = 1500;
    public int MaxNewMaterialsPerRun { get; set; } = 10;
    public int MaxItemsPerSource { get; set; } = 20;

    public int GetDelayMs(string host) =>
        HostDelaysMs.TryGetValue(host, out var ms) ? ms : HostDelayMs;
}