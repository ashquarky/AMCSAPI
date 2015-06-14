Namespace Exceptions
    Public Class LoginFailedException
        Inherits System.ApplicationException
        ''This is really all I need
        Public Sub New(msg As String, inner As System.Exception)
            MyBase.New(msg, inner)
        End Sub
        Public Sub New(msg As String)
            MyBase.New(msg)
        End Sub
    End Class
End Namespace
