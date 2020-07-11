namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2HostOptions
    {
        public const string SectionName = "Aria2Startup";

        public int? ListenPort { get; set; }

        public int RefreshInterval { get; set; } = 1000;

        public string? ExecutablePath { get; set; }

        public string? WorkingDirectory { get; set; }

        public string? Token { get; set; }
    }
}
