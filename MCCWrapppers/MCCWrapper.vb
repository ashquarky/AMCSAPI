Imports MinecraftClient.Protocol
Imports MinecraftClient
Imports System.Linq
Namespace MCCWrappers
    ''A stripped-down port of the MCC main class
    ''Built from MinecraftClient/Program.cs as of 0c81c703db1d42098261cf0245fca99471c6e63c
    Friend Class MCCWrapper
        ''This whole structure thing is unnecessary, but I'm trying to follow the actual MCC class as close as possible.
        Friend Structure Settings_template
            Public Password As String
            Public Login As String
            Public Username As String
            Public ServerIP As String
            Public ServerPort As UShort
        End Structure

        Dim WithEvents client As McTcpClient
        Dim Settings As New Settings_template

        Event textReceived(args As EventArgs.ChatReceivedEventArgs)

        Friend Sub init(username As String, password As String, ip As String, port As UShort)
            Dim result As ProtocolHandler.LoginResult
            Settings.Login = username
            Settings.Password = password
            Settings.ServerIP = ip
            Settings.Username = Settings.Login ''Settings.Username will be overwritten at runtime
            Settings.ServerPort = port
            Dim sessionID As String = ""
            Dim UUID As String = ""
            If Settings.Password = "-" Then
                ''OfflineMode!
                result = ProtocolHandler.LoginResult.Success
                sessionID = "0"
            Else
                ''Online mode!
                result = ProtocolHandler.GetLogin(Settings.Username, Settings.Password, sessionID, UUID)
            End If
            If result = ProtocolHandler.LoginResult.Success Then
                ''Success!
                Dim protocolVersion As Integer
                ''TODO: in the original class, this was only if set to Auto version detect.
                If ProtocolHandler.GetServerInfo(Settings.ServerIP, Settings.ServerPort, protocolVersion) = False Then
                    ''Failed to ping IP...
                    Dim ex As New System.Runtime.Remoting.ServerException("Couldn't reach Minecraft server. See Data for details.")
                    ex.Data("ServerIP") = Settings.ServerIP
                    ex.Data("ServerPort") = Settings.ServerPort
                    ex.Data("protocolVersion") = protocolVersion
                    Throw ex
                End If
                If protocolVersion <> 0 Then
                    Try
                        client = New AMCSAPI.MCCWrappers.McTcpClient(Settings.Username, UUID, sessionID, protocolVersion, Settings.ServerIP, Settings.ServerPort)
                    Catch ex As System.NotSupportedException
                        ex.Data("ServerIP") = Settings.ServerIP
                        ex.Data("ServerPort") = Settings.ServerPort
                        ex.Data("protocolVersion") = protocolVersion
                        ex.Data("UUID") = UUID
                        ex.Data("sessionID") = sessionID
                        Throw New Exceptions.UnsupportedVersionException("Server is not a supported Minecraft version. See InnerException's Data for details.", ex)
                    End Try
                End If
            ElseIf result = ProtocolHandler.LoginResult.WrongPassword Then
                Throw New System.Security.Authentication.InvalidCredentialException("Incorrect Minecraft/Mojang account password.")
            Else
                Dim ex As New Exceptions.LoginFailedException("Login Failed. See Data for details.")
                ex.Data("FailureReason") = result
                Throw ex
            End If
        End Sub
        Private Sub client_textReceived(text As String) Handles client.textReceived
            Dim args As New EventArgs.ChatReceivedEventArgs(text)
            RaiseEvent textReceived(args)
        End Sub
        Friend Sub Disconnect()
            client.Disconnect()
        End Sub
        Friend Sub SendText(text As String)
            If client.getConnectionStatus = False Then
                Throw New System.InvalidOperationException("Client is not currently connected to a server.")
            End If
            client.SendText(text)
        End Sub
    End Class
End Namespace
