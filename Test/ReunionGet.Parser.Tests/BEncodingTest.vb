Imports Xunit
Imports System.Text

Public Class BEncodingTest
    Private Function ReadFromString(s As String) As Object
        Dim bytes = Encoding.UTF8.GetBytes(s)
        Return BEncoding.Read(bytes)
    End Function

    <Fact>
    Public Sub TestInteger()
        Dim o = ReadFromString("i123e")
        Assert.Equal(123, o)
    End Sub

    <Fact>
    Public Sub TestContentAfterEnding()
        Assert.Throws(Of ArgumentException)(
            Sub() ReadFromString("i123eaaa"))
    End Sub

    <Fact>
    Public Sub TestString()
        Dim o = ReadFromString("5:abcde")
        Assert.Equal("abcde", o)
    End Sub

    <Fact>
    Public Sub TestList()
        Dim o = ReadFromString("li123e3:abci4ee")
        Assert.Equal(New Object() {123, "abc", 4}, o)
    End Sub

    <Fact>
    Public Sub TestEmptyList()
        Dim o = ReadFromString("le")
        Assert.Equal(New Object() {}, o)
    End Sub

    <Fact>
    Public Sub TestDict()
        Dim o = ReadFromString("d1:ai123e1:b3:abce")
        Assert.Equal(New Dictionary(Of String, Object) From {
            {"a", 123},
            {"b", "abc"}
        }, o)
    End Sub

    <Fact>
    Public Sub TestEmptyDict()
        Dim o = ReadFromString("de")
        Assert.Equal(New Dictionary(Of String, Object), o)
    End Sub

    <Fact>
    Public Sub TestMultiLevelDict()
        Dim o = ReadFromString("d1:ai123e1:bd2:cdli456e3:efgeee")
        Assert.Equal(New Dictionary(Of String, Object) From {
            {"a", 123},
            {"b", New Dictionary(Of String, Object) From {
                {"cd", New Object() {456, "efg"}}
            }}
        }, o)
    End Sub

    <Fact>
    Public Sub TestKeyMustBeString()
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString("di123ei456ee"))
    End Sub

    <Theory>
    <InlineData("i123")>
    <InlineData("5:abcd")>
    <InlineData("li123e3:abci4e")>
    Public Sub TestNotEnoughData(value As Object)
        Assert.ThrowsAny(Of Exception)(
            Sub() ReadFromString(CStr(value)))
    End Sub
End Class
