Imports Xunit

Public Class MagnetTest
    <Fact>
    Public Sub TestBtih()
        Const BtihMagnet = "magnet:?xt=urn:btih:D0D14C926E6E99761A2FDCFF27B403D96376EFF6"
        Dim magnet = New Magnet(BtihMagnet)
        Assert.Equal(MagnetHashAlgorithm.BTIH, magnet.HashAlgorithm)
        Assert.True(magnet.Hash.SequenceEqual(HexToBytes("D0D14C926E6E99761A2FDCFF27B403D96376EFF6")))
    End Sub
End Class
