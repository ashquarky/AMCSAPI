Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net.Sockets
Imports System.Threading
Imports System.IO
Imports System.Net
Imports MinecraftClient
Imports MinecraftClient.Protocol
Imports MinecraftClient.Proxy
Imports System
Namespace MCCWrappers


    ''' <summary>
    ''' Rewritten MCC class.
    ''' Bots functionality removed.
    ''' CommandPrompt removed.
    ''' </summary>

    Friend Class McTcpClient
        Implements IMinecraftComHandler
        Private Shared cmd_names As New List(Of String)()
        Private Shared cmds As New Dictionary(Of String, Command)()
        Private onlinePlayers As New Dictionary(Of Guid, String)()
        Private Shared scripts_on_hold As New List(Of ChatBots.Script)()

        Public Shared AttemptsLeft As Integer = 0

        Event textReceived(text As String)
        Event connectionLost(reason As ChatBot.DisconnectReason, message As String)

        Private host As String
        Private port As Integer
        Private username As String
        Private uuid As String
        Private sessionid As String

        Public Function getServerPort() As Integer Implements IMinecraftComHandler.getServerPort
            Return port
        End Function
        Public Function getServerHost() As String Implements IMinecraftComHandler.getServerHost
            Return host
        End Function
        Public Function getUsername() As String Implements IMinecraftComHandler.getUsername
            Return username
        End Function
        Public Function getUserUUID() As String Implements IMinecraftComHandler.getUserUUID
            Return uuid
        End Function
        Public Function getSessionID() As String Implements IMinecraftComHandler.getSessionID
            Return sessionid
        End Function

        Private client As TcpClient
        Private handler As IMinecraftCom
        Private cmdprompt As Thread

        ''' <summary>
        ''' Starts the main chat client
        ''' </summary>
        ''' <param name="username">The chosen username of a premium Minecraft Account</param>
        ''' <param name="uuid">The player's UUID for online-mode authentication</param>
        ''' <param name="sessionID">A valid sessionID obtained after logging in</param>
        ''' <param name="server_ip">The server IP</param>
        ''' <param name="port">The server port to use</param>
        ''' <param name="protocolversion">Minecraft protocol version to use</param>

        Public Sub New(username As String, uuid As String, sessionID As String, protocolversion As Integer, server_ip As String, port As UShort)
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, _
             False, "")
        End Sub

        ''' <summary>
        ''' Starts the main chat client in single command sending mode
        ''' </summary>
        ''' <param name="username">The chosen username of a premium Minecraft Account</param>
        ''' <param name="uuid">The player's UUID for online-mode authentication</param>
        ''' <param name="sessionID">A valid sessionID obtained after logging in</param>
        ''' <param name="server_ip">The server IP</param>
        ''' <param name="port">The server port to use</param>
        ''' <param name="protocolversion">Minecraft protocol version to use</param>
        ''' <param name="command">The text or command to send.</param>

        Public Sub New(username As String, uuid As String, sessionID As String, server_ip As String, port As UShort, protocolversion As Integer, _
         command As String)
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, _
             True, command)
        End Sub

        ''' <summary>
        ''' Starts the main chat client, wich will login to the server using the MinecraftCom class.
        ''' </summary>
        ''' <param name="user">The chosen username of a premium Minecraft Account</param>
        ''' <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        ''' <param name="server_ip">The server IP</param>
        ''' <param name="port">The server port to use</param>
        ''' <param name="protocolversion">Minecraft protocol version to use</param>
        ''' <param name="uuid">The player's UUID for online-mode authentication</param>
        ''' <param name="singlecommand">If set to true, the client will send a single command and then disconnect from the server</param>
        ''' <param name="command">The text or command to send. Will only be sent if singlecommand is set to true.</param>

        Private Sub StartClient(user As String, uuid As String, sessionID As String, server_ip As String, port As UShort, protocolversion As Integer, _
         singlecommand As Boolean, command As String)
            Me.sessionid = sessionID
            Me.uuid = uuid
            Me.username = user
            Me.host = server_ip
            Me.port = port

            Try
                client = ProxyHandler.newTcpClient(host, port)
                client.ReceiveBufferSize = 1024 * 1024
                handler = Protocol.ProtocolHandler.getProtocolHandler(client, protocolversion, Me)
                ''Version supported, logging in...

                If handler.Login() Then
                    scripts_on_hold.Clear()

                    ''Server joined.

                End If
            Catch generatedExceptionName As SocketException
                Console.WriteLine("Failed to connect to this IP.")
                If AttemptsLeft > 0 Then
                    ChatBot.LogToConsole("Waiting 5 seconds (" + AttemptsLeft + " attempts left)...")
                    Thread.Sleep(5000)
                    AttemptsLeft -= 1

                ElseIf Not singlecommand Then
                    Console.ReadLine()
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Stub.
        ''' </summary>

        Public Function performInternalCommand(command__1 As String, ByRef response_msg As String) As Boolean
            Return True
        End Function

        ''' <summary>
        ''' Disconnect the client from the server
        ''' </summary>

        Public Sub Disconnect()

            If handler IsNot Nothing Then
                handler.Disconnect()
                handler.Dispose()
            End If

            If cmdprompt IsNot Nothing Then
                cmdprompt.Abort()
            End If

            Thread.Sleep(1000)

            If client IsNot Nothing Then
                client.Close()
            End If
        End Sub

        ''' <summary>
        ''' Received some text from the server
        ''' </summary>
        ''' <param name="text">Text received</param>

        Public Sub OnTextReceived(text As String) Implements IMinecraftComHandler.OnTextReceived
            RaiseEvent textReceived(text)
        End Sub

        ''' <summary>
        ''' When connection has been lost
        ''' </summary>

        Public Sub OnConnectionLost(reason As ChatBot.DisconnectReason, message As String) Implements IMinecraftComHandler.OnConnectionLost
            RaiseEvent connectionLost(reason, message)
        End Sub

        ''' <summary>
        ''' Called ~10 times per second by the protocol handler
        ''' </summary>

        Public Sub OnUpdate() Implements IMinecraftComHandler.OnUpdate
            'Tasty.
        End Sub

        ''' <summary>
        ''' Send a chat message or command to the server
        ''' </summary>
        ''' <param name="text">Text to send to the server</param>
        ''' <returns>True if the text was sent with no error</returns>

        Public Function SendText(text As String) As Boolean
            If text.Length > 100 Then
                'Message is too long?
                If text(0) = "/"c Then
                    'Send the first 100 chars of the command
                    text = text.Substring(0, 100)
                    Return handler.SendChatMessage(text)
                Else
                    'Send the message splitted into several messages
                    While text.Length > 100
                        handler.SendChatMessage(text.Substring(0, 100))
                        text = text.Substring(100, text.Length - 100)
                    End While
                    Return handler.SendChatMessage(text)
                End If
            Else
                Return handler.SendChatMessage(text)
            End If
        End Function

        ''' <summary>
        ''' Allow to respawn after death
        ''' </summary>
        ''' <returns>True if packet successfully sent</returns>

        Public Function SendRespawnPacket() As Boolean
            Return handler.SendRespawnPacket()
        End Function

        ''' <summary>
        ''' Triggered when a new player joins the game
        ''' </summary>
        ''' <param name="uuid">UUID of the player</param>
        ''' <param name="name">Name of the player</param>

        Public Sub OnPlayerJoin(uuid As Guid, name As String) Implements IMinecraftComHandler.OnPlayerJoin
            onlinePlayers(uuid) = name
        End Sub

        ''' <summary>
        ''' Triggered when a player has left the game
        ''' </summary>
        ''' <param name="uuid">UUID of the player</param>

        Public Sub OnPlayerLeave(uuid As Guid) Implements IMinecraftComHandler.OnPlayerLeave
            onlinePlayers.Remove(uuid)
        End Sub

        ''' <summary>
        ''' Get a set of online player names
        ''' </summary>
        ''' <returns>Online player names</returns>

        Public Function getOnlinePlayers() As String() Implements IMinecraftComHandler.getOnlinePlayers
            Return onlinePlayers.Values.Distinct().ToArray()
        End Function

        Public Function getConnectionStatus() As Boolean
            Try
                Return client.Connected
            Catch ex As Exception
                ''Maybe variable wasn't initialised?
                Return False
            End Try
        End Function
    End Class
End Namespace

