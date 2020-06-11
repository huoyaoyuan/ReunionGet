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

            Assert.Equal(magnet, torrent.ToMagnet())
            Assert.True(magnet.Fits(torrent))
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
End Class
