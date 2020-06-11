Imports Xunit
Imports System.Text

Public Class BEncodingTest
    Private Shared Function ReadFromString(s As String) As Object
        Dim bytes = Encoding.UTF8.GetBytes(s)
        Return BEncoding.Read(bytes, True)
    End Function

    <Theory>
    <InlineData("i123e", 123L)>
    <InlineData("i-123e", -123L)>
    <InlineData("i0e", 0L)>
    <InlineData("i1145141919810e", 1145141919810L)>
    Public Sub TestIntegers(value As Object, expected As Object)
        Dim o = ReadFromString(CStr(value))
        Assert.Equal(expected, o)
    End Sub

    <Theory>
    <InlineData("i01e")>
    <InlineData("i-0e")>
    <InlineData("i1.2e")>
    Public Sub TestInvalidNumbers(value As Object)
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString(CStr(value)))
    End Sub

    <Fact>
    Public Sub TestContentAfterEnding()
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString("i123eaaa"))
    End Sub

    <Fact>
    Public Sub TestString()
        Dim o = ReadFromString("5:abcde")
        Assert.Equal("abcde", o)
    End Sub

    <Fact>
    Public Sub TestLongString()
        Dim o = ReadFromString("12:qwertyuiop[]")
        Assert.Equal("qwertyuiop[]", o)
    End Sub

    <Fact>
    Public Sub TestList()
        Dim o = ReadFromString("li123e3:abci4ee")
        Assert.Equal(New Object() {123L, "abc", 4L}, o)
    End Sub

    <Fact>
    Public Sub TestEmptyList()
        Dim o = ReadFromString("le")
        Assert.Equal(Array.Empty(Of Object)(), o)
    End Sub

    <Fact>
    Public Sub TestDict()
        Dim o = ReadFromString("d1:ai123e1:b3:abce")
        Assert.Equal(New Dictionary(Of String, Object) From {
            {"a", 123L},
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
            {"a", 123L},
            {"b", New Dictionary(Of String, Object) From {
                {"cd", New Object() {456L, "efg"}}
            }}
        }, o)
    End Sub

    <Fact>
    Public Sub TestKeyMustBeString()
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString("di123ei456ee"))
    End Sub

    <Fact>
    Public Sub TestUnorderedKey()
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString("d1:bi1e1:ai1ee"))
    End Sub

    <Theory>
    <InlineData("i123")>
    <InlineData("5:abcd")>
    <InlineData("li123e3:abci4e")>
    <InlineData("e")>
    <InlineData("ie")>
    <InlineData("i-e")>
    Public Sub TestNotEnoughData(value As Object)
        Assert.Throws(Of FormatException)(
            Sub() ReadFromString(CStr(value)))
    End Sub

    <Theory>
    <InlineData("🕊")>
    <InlineData("🐎")>
    <InlineData("🀇")>
    Public Sub TestUtf8(value As Object)
        Dim u8Str = CStr(value)
        Dim u8Len = Encoding.UTF8.GetByteCount(u8Str)
        Dim bytes = Encoding.UTF8.GetBytes(u8Len & ":" & u8Str)
        Dim o = BEncoding.Read(bytes)
        Assert.Equal(u8Str, o)
    End Sub

    <Theory>
    <InlineData("i00e", 0L)>
    <InlineData("i01e", 1L)>
    <InlineData("i-0e", 0L)>
    <InlineData("ie", 0L)>
    <InlineData("i-e", 0L)>
    Public Sub TestNonStrictNumber(value As Object, expected As Object)
        Dim bytes = Encoding.UTF8.GetBytes(CStr(value))
        Dim o = BEncoding.Read(bytes, False)
        Assert.Equal(expected, o)
    End Sub

    <Fact>
    Public Sub TestNonStrictDict()
        Dim bytes = Encoding.UTF8.GetBytes(CStr("d1:bi1e1:ai1ee"))
        Dim o = BEncoding.Read(bytes, False)
        Assert.Equal(New Dictionary(Of String, Object) From {
            {"b", 1L},
            {"a", 1L}
        }, o)
    End Sub
End Class
