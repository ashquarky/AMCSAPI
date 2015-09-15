Imports System.Text.RegularExpressions

Namespace InfoStructures
    Public Class ChatReceivedEventArgs
        ''' <summary>
        ''' The raw chat message - colour codes, white space and all.
        ''' </summary>
        ''' <remarks>I advise you run it through a String.Trim before you use this variable</remarks>
        Public messageRaw As String
        ''' <summary>
        ''' The chat message with colour codes and useless whitespace stripped.
        ''' </summary>
        ''' <remarks>Use messageRaw for colour codes.</remarks>
        Public message As String

        Friend Sub New(msg As String)
            messageRaw = msg
            message = Regex.Replace(msg, "§.", "")
        End Sub
    End Class
End Namespace
