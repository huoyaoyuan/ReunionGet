using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Responses
{
    public sealed class Aria2VersionInfo
    {
        [JsonConstructor]
        public Aria2VersionInfo(string version, IReadOnlyList<string> enabledFeatures)
        {
            Version = version;
            EnabledFeatures = enabledFeatures;
        }

        public string Version { get; }

        public IReadOnlyList<string> EnabledFeatures { get; }
    }
}
