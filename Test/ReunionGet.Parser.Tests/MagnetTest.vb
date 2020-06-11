Imports Xunit

Public Class MagnetTest
    <Theory>
    <InlineData("magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6")>
    <InlineData("magnet:?xt=urn:btih:2DIUZETON2MXMGRP3T7SPNAD3FRXN37W")>
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
End Class
