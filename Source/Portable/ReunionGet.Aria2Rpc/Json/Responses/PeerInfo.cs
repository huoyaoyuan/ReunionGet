using System.Collections;
using System.Net;
using System.Text.Json.Serialization;
using ReunionGet.Aria2Rpc.Json.Converters;

namespace ReunionGet.Aria2Rpc.Json.Responses
{
    public sealed class PeerInfo
    {
        [JsonConstructor]
        public PeerInfo(
            string peerId, string ip, int port, BitArray? bitfield, bool amChoking, bool peerChoking, int downloadSpeed,
            int uploadSpeed, bool seeder)
        {
            PeerId = peerId;
            Ip = ip;
            Port = port;
            Bitfield = bitfield;
            AmChoking = amChoking;
            PeerChoking = peerChoking;
            DownloadSpeed = downloadSpeed;
            UploadSpeed = uploadSpeed;
            Seeder = seeder;
        }

        [JsonConverter(typeof(UrlEncodeStringConverter))]
        public string PeerId { get; }

        public string Ip { get; }

        public int Port { get; }

        [JsonIgnore]
        public IPEndPoint EndPoint => new IPEndPoint(IPAddress.Parse(Ip), Port);

        /// <summary>
        /// Hexadecimal representation of the download progress of the peer.
        /// </summary>
        /// <seealso cref="DownloadProgressStatus.Bitfield"/>
        [JsonConverter(typeof(HexBitArrayConverter))]
        public BitArray? Bitfield { get; }

        /// <summary>
        /// If aria2 is choking the peer.
        /// </summary>
        public bool AmChoking { get; }

        /// <summary>
        /// If the peer is choking aria2. 
        /// </summary>
        public bool PeerChoking { get; }

        public int DownloadSpeed { get; }

        public int UploadSpeed { get; }

        public bool Seeder { get; }
    }
}
