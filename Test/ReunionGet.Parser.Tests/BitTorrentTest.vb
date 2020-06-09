Imports System.Text
Imports Xunit

Public Class BitTorrentTest
    <Fact>
    Public Sub TestTorrent()
        Using resourceStream = GetType(BitTorrentTest).Assembly.GetManifestResourceStream(GetType(BitTorrentTest), "sample.torrent")
            Assert.NotNull(resourceStream)
            Dim torrent = BitTorrent.FromStream(resourceStream)

            Assert.Equal("udp://tracker.openbittorrent.com:80/", torrent.Announce.AbsoluteUri)
            Assert.True(torrent.IsSingleFile)
            Assert.Equal(20, torrent.SingleFileLength)
            Assert.Null(torrent.Files)
            Assert.Equal(65536, torrent.PieceLength)
            Assert.Equal("sample.txt", torrent.Name)
            Assert.Equal(1, torrent.PieceHashes.Length)
            Assert.Equal("5CC5E652BE0DE6F27805B30464FF9B00F489F0C9", torrent.PieceHashes(0).ToString())
            Assert.Equal(torrent.TotalLength, torrent.SingleFileLength)
            Assert.True(torrent.IsPrivate)
            Assert.Equal(#2012/1/20 8:57:07#, torrent.CreationTime.Value.UtcDateTime)
            Assert.Equal("D0D14C926E6E99761A2FDCFF27B403D96376EFF6", torrent.InfoHash.ToString())
        End Using
    End Sub

    <Fact>
    Public Sub TestTorrentAsObject()
        Dim hash = New Byte() {
            &H5C, &HC5, &HE6, &H52, &HBE,
            &HD, &HE6, &HF2, &H78, &H5,
            &HB3, &H4, &H64, &HFF, &H9B,
            &H0, &HF4, &H89, &HF0, &HC9}
        Dim hashStr = Encoding.UTF8.GetString(hash)

        Using resourceStream = GetType(BitTorrentTest).Assembly.GetManifestResourceStream(GetType(BitTorrentTest), "sample.torrent")
            Assert.NotNull(resourceStream)
            Dim length = CInt(resourceStream.Length)
            Dim bytes(length - 1) As Byte
            resourceStream.Read(bytes)
            Dim o = BEncoding.Read(bytes)

            Assert.Equal(New Dictionary(Of String, Object) From {
                {"announce", "udp://tracker.openbittorrent.com:80"},
                {"creation date", 1327049827L},
                {"info", New Dictionary(Of String, Object) From {
                    {"length", 20L},
                    {"name", "sample.txt"},
                    {"pieces", hashStr},
                    {"piece length", 65536L},
                    {"private", 1L}
                }}
            }, o)
        End Using
    End Sub
End Class
