Namespace InfoStructures
    Public Class ServerInformation ''This SO needs a better name
        ''' <summary>
        ''' The version name returned by the server. Usually the name of the server software (Glowstone, Bukkit, Sponge etc.)
        ''' </summary>
        ''' <remarks>Will only be 1.7 or 1.8.</remarks>
        Public serverVersionName As String ''This is usally a name like "Spigot 1.8" or "CraftBukkit"
        ''' <summary>
        ''' The actual version of the server, determined from protocolVersion. Can be innacurate on some servers.
        ''' Where multiple versions are compatible, a range will be returned (e.g. 1.7.2-1.7.5)
        ''' </summary>
        Public serverVersion As String
        ''' <summary>
        ''' The protocol version returned from the server.
        ''' </summary>
        Public protocolVersion As Integer
        ''' <summary>
        ''' The number of online players as returned by the server.
        ''' </summary>
        Public onlinePlayers As Integer
        ''' <summary>
        ''' The maximum number of players as returned by the server.
        ''' </summary>
        Public maxPlayers As Integer
        ''' <summary>
        ''' The raw description of the server, all formatting codes included.
        ''' </summary>
        Public descriptionRaw As String
        Friend Sub setVersionFromProtocol()
            Select Case protocolVersion
                Case 51 ''Why do I have methods for versions this old? Don't ask.
                    serverVersion = "1.4.6-1.4.7"
                Case 60 ''To be honest, I don't think a 1.5 server would even respond to this ping packet. Oh well.
                    serverVersion = "1.5.1"
                Case 61
                    serverVersion = "1.5.2"
                Case 72
                    serverVersion = "1.6.0"
                Case 73
                    serverVersion = "1.6.1-1.6.4"
                Case 4
                    serverVersion = "1.7.2-1.7.5"
                Case 5
                    serverVersion = "1.7.6-1.7.10"
                Case 47
                    serverVersion = "1.8.0-1.8.8"
            End Select
        End Sub
    End Class
End Namespace