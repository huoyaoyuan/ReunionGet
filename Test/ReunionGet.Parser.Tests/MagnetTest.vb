Imports Xunit

Public Class MagnetTest
    Private Const SHA1Magnet As String = "magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6"
    Private Const Base32Magnet As String = "magnet:?xt=urn:btih:2DIUZETON2MXMGRP3T7SPNAD3FRXN37W"

    <Theory>
    <InlineData(SHA1Magnet)>
    <InlineData(Base32Magnet)>
    Public Sub TestBtih(param As Object)
        Dim magnet = New Magnet(CStr(param))
        Assert.Equal(MagnetHashAlgorithm.BTIH, magnet.HashAlgorithm)
        Assert.Equal("D0D14C926E6E99761A2FDCFF27B403D96376EFF6", magnet.HashValue.ToString())
    End Sub

    <Theory>
    <InlineData("magnet:")>
    <InlineData("http://example.com")>
    <InlineData("magnet:?xt=nothing")>
    <InlineData("magnet:?xt=urn:wtf:D0D14C926E6E99761A2FDCFF27B403D96376EFF6")>
    <InlineData("magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF")>
    <InlineData("magnet:?xt=urn:btih:D0D14C926E6E9976#A2FDCFF27B403D96376EFF6")>
    <InlineData("magnet:?xt=urn:btih:2DIUZETON2MXMGRP3T7SPNAD3FRXN37=")>
    <InlineData("magnet:?xt=urn:btih:2DIUZETON2MXMGRP#T7SPNAD3FRXN37W")>
    Public Sub TestInvalidMagnet(param As Object)
        Assert.Throws(Of MagnetFormatException)(
            Sub()
                Dim magnet = New Magnet(CStr(param))
            End Sub)
    End Sub

    <Fact>
    Public Sub TestTorrentMagnetInteraction()
        Using resourceStream = GetType(BitTorrentTest).Assembly.GetManifestResourceStream(GetType(BitTorrentTest), "sample.torrent")
            Assert.NotNull(resourceStream)
            Dim torrent = BitTorrent.FromStream(resourceStream)
            Dim magnet = New Magnet(SHA1Magnet)

            Dim toMagnet As Magnet = torrent.ToMagnet()
            Assert.Equal(magnet, toMagnet)
            Assert.True(magnet.Fits(torrent))
            Assert.False(toMagnet.ExactEquals(magnet))

            Assert.Equal(torrent.Name, toMagnet.DisplayName)
            Assert.Equal(torrent.SingleFileLength, toMagnet.ExactLength)
            Assert.Null(toMagnet.OriginalString)
            Assert.Equal(New Uri() {New Uri("udp://tracker.openbittorrent.com:80/")}, toMagnet.Trackers)
            Assert.Null(toMagnet.ExactSource)
            Assert.Empty(toMagnet.AcceptableSources)
            Assert.Empty(toMagnet.KeywordTopic)
            Assert.Null(toMagnet.ManifestTopic)

            Assert.Equal(SHA1Magnet, toMagnet.ToString())
            Assert.Equal(Base32Magnet, toMagnet.ToStringBase32())
            Assert.Equal("magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6&dn=sample.txt&tr=udp%3a%2f%2ftracker.openbittorrent.com%3a80%2f&xl=20",
                         toMagnet.ToFullString())
        End Using
    End Sub

    <Fact>
    Public Sub TestMagnetFormats()
        Dim magnet1 = New Magnet(SHA1Magnet)
        Dim magnet2 = New Magnet(Base32Magnet)

        Assert.Equal(magnet1, magnet2)
        Assert.True(magnet1 = magnet2)
        Assert.NotEqual(magnet1.OriginalString, magnet2.OriginalString)
        Assert.Equal(Base32Magnet, magnet1.ToStringBase32())
        Assert.Equal(SHA1Magnet, magnet2.ToString())
    End Sub

    <Fact>
    Public Sub TestMagnetParts()
        Const FullMagnet = "magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6&dn=sample.txt" &
            "&as=http%3a%2f%2fexample.com%2fsample1.txt" &
            "&as=http%3a%2f%2fexample.com%2fsample2.txt" &
            "&kt=example%2btxt" &
            "&tr=udp%3a%2f%2ftracker.openbittorrent.com%3a80%2f&xl=20" &
            "&xs=http%3a%2f%2fexample.com%2fsample.txt" &
            "&mt=http%3a%2f%2fexample.com%2fexampletopic"

        Dim m = New Magnet(FullMagnet)

        Assert.Equal(FullMagnet, m.OriginalString)
        Assert.Equal(New Uri() {New Uri("udp://tracker.openbittorrent.com:80/")}, m.Trackers)
        Assert.Equal(20L, m.ExactLength)
        Assert.Equal("sample.txt", m.DisplayName)
        Assert.Equal(New Uri("http://example.com/sample.txt"), m.ExactSource)
        Assert.Equal(New Uri() {
                         New Uri("http://example.com/sample1.txt"),
                         New Uri("http://example.com/sample2.txt")
                     }, m.AcceptableSources)
        Assert.Equal(New String() {"example", "txt"}, m.KeywordTopic)
        Assert.Equal(New Uri("http://example.com/exampletopic"), m.ManifestTopic)

        Assert.NotEqual(FullMagnet, m.ToFullString())
        Assert.True(m.ExactEquals(New Magnet(m.ToFullString())))
    End Sub
End Class
