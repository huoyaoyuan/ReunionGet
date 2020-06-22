using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Responses
{
    public sealed class GlobalStat
    {
        [JsonConstructor]
        public GlobalStat(
            int downloadSpeed,
            int uploadSpeed,
            int numActive,
            int numWaiting,
            int numStopped,
            int numStoppedTotal)
        {
            DownloadSpeed = downloadSpeed;
            UploadSpeed = uploadSpeed;
            NumActive = numActive;
            NumWaiting = numWaiting;
            NumStopped = numStopped;
            NumStoppedTotal = numStoppedTotal;
        }

        public int DownloadSpeed { get; }

        public int UploadSpeed { get; }

        public int NumActive { get; }

        public int NumWaiting { get; }

        public int NumStopped { get; }

        public int NumStoppedTotal { get; }
    }
}
