Imports Xunit

Public Class MagnetTest
    <Theory>
    <InlineData("magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6")>
    <InlineData("magnet:?xt=urn:btih:2DIUZETON2MXMGRP3T7SPNAD3FRXN37W")>
    Public Sub TestBtih(param As Object)
        Dim magnet = New Magnet(CStr(param))
        Assert.Equal(MagnetHashAlgorithm.BTIH, magnet.HashAlgorithm)
        Assert.True(magnet.Hash.SequenceEqual(HexToBytes("D0D14C926E6E99761A2FDCFF27B403D96376EFF6")))
    End Sub
End Class
