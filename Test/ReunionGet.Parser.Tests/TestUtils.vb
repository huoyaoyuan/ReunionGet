Imports System.Globalization

Friend Module TestUtils
    Public Function HexToBytes(hexStr As String) As Byte()
        Dim result(hexStr.Length \ 2 - 1) As Byte
        For i = 0 To result.Length - 1
            result(i) = Byte.Parse(hexStr.AsSpan(i * 2, 2), NumberStyles.HexNumber)
        Next
        Return result
    End Function
End Module
