Imports System.Buffers
Imports System.Text
Imports Xunit

Public Class Base32Test
    <Theory>
    <InlineData("Base32 test string", "IJQXGZJTGIQHIZLTOQQHG5DSNFXGO===")>
    Public Sub TestEncoding(plainStr As Object, base32Str As Object)
        Dim plainArray = Encoding.UTF8.GetBytes(CStr(plainStr))
        Dim base32Array = Encoding.UTF8.GetBytes(CStr(base32Str))

        Assert.Equal(base32Array.Length, Base32.GetMaxEncodedToUtf8Length(plainArray.Length))
        Assert.True(plainArray.Length <= Base32.GetMaxDecodedFromUtf8Length(base32Array.Length))

        Dim buffer(base32Array.Length - 1) As Byte
        Dim bytesConsumed As Integer, bytesWritten As Integer
        Dim result = Base32.EncodeToUtf8(plainArray, buffer, bytesConsumed, bytesWritten)

        Assert.Equal(OperationStatus.Done, result)
        Assert.Equal(plainArray.Length, bytesConsumed)
        Assert.Equal(base32Array.Length, bytesWritten)
        Assert.Equal(base32Array, buffer)
    End Sub

    <Theory>
    <InlineData("Base32 test string", "IJQXGZJTGIQHIZLTOQQHG5DSNFXGO===")>
    Public Sub TestEncodingUtf16(plainStr As Object, base32Str As Object)
        Dim plainArray = Encoding.UTF8.GetBytes(CStr(plainStr))
        Dim base32String = CStr(base32Str)

        Assert.Equal(base32String.Length, Base32.GetMaxEncodedToUtf8Length(plainArray.Length))
        Assert.True(plainArray.Length <= Base32.GetMaxDecodedFromUtf8Length(base32String.Length))

        Dim buffer(base32String.Length - 1) As Char
        Dim bytesConsumed As Integer, bytesWritten As Integer
        Dim result = Base32.EncodeToUtf16(plainArray, buffer, bytesConsumed, bytesWritten)

        Assert.Equal(OperationStatus.Done, result)
        Assert.Equal(plainArray.Length, bytesConsumed)
        Assert.Equal(base32String.Length, bytesWritten)
        Assert.Equal(base32String, buffer)
    End Sub

    <Theory>
    <InlineData("Base32 test string", "IJQXGZJTGIQHIZLTOQQHG5DSNFXGO===")>
    Public Sub TestDecoding(plainStr As Object, base32Str As Object)
        Dim plainArray = Encoding.UTF8.GetBytes(CStr(plainStr))
        Dim base32Array = Encoding.UTF8.GetBytes(CStr(base32Str))

        Assert.True(plainArray.Length <= Base32.GetMaxDecodedFromUtf8Length(base32Array.Length))

        Dim buffer(plainArray.Length - 1) As Byte
        Dim bytesConsumed As Integer, bytesWritten As Integer
        Dim result = Base32.DecodeFromUtf8(base32Array, buffer, bytesConsumed, bytesWritten)

        Assert.Equal(OperationStatus.Done, result)
        Assert.Equal(base32Array.Length, bytesConsumed)
        Assert.Equal(plainArray.Length, bytesWritten)
        Assert.Equal(plainArray, buffer)
    End Sub

    <Theory>
    <InlineData("Base32 test string", "IJQXGZJTGIQHIZLTOQQHG5DSNFXGO===")>
    Public Sub TestDecodingUtf16(plainStr As Object, base32Str As Object)
        Dim plainArray = Encoding.UTF8.GetBytes(CStr(plainStr))
        Dim base32String = CStr(base32Str)

        Assert.True(plainArray.Length <= Base32.GetMaxDecodedFromUtf8Length(base32String.Length))

        Dim buffer(plainArray.Length - 1) As Byte
        Dim bytesConsumed As Integer, bytesWritten As Integer
        Dim result = Base32.DecodeFromUtf16(base32String, buffer, bytesConsumed, bytesWritten)

        Assert.Equal(OperationStatus.Done, result)
        Assert.Equal(base32String.Length, bytesConsumed)
        Assert.Equal(plainArray.Length, bytesWritten)
        Assert.Equal(plainArray, buffer)
    End Sub
End Class
