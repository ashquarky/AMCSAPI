Imports AMCSAPI.MCCWrappers

''' <summary>
''' Main class. Used to represent a server. Everything you do with AMCSAPI starts here.
''' </summary>
Public Class Server
    Private WithEvents chat As MCCWrapper
    Private settings As New MCCWrapper.Settings_template

    ''' <summary>
    ''' Called when a new chat message is received.
    ''' You must have an active chat connection running.
    ''' </summary>
    ''' <param name="args">The chat message.</param>
    Event chatMessageReceived(args As EventArgs.ChatReceivedEventArgs)

    ''' <summary>
    ''' Initialise everything and stores server info for future use.
    ''' </summary>
    ''' <param name="serverIP">IP or hostname of server.</param>
    ''' <param name="port">Port number of server.</param>
    ''' <remarks>For now, version is inferred.</remarks>
    Public Sub New(serverIP As String, port As UShort)
        settings.ServerIP = serverIP
        settings.ServerPort = port
        chat = New MCCWrapper
    End Sub

    ''' <summary>
    ''' Store username and password for future use.
    ''' </summary>
    ''' <param name="username">Minecraft/Mojang account username.</param>
    ''' <param name="password">Minecraft/Mojang account password.</param>
    ''' <remarks>This doesn't actually authenticate with the Minecraft login servers, simply stores it for later.</remarks>
    Public Sub authenticate(username As String, password As String)
        settings.Username = username
        settings.Password = password
    End Sub

    ''' <summary>
    ''' Open a chat connection to the server. Will raise chatMessageReceived event when message received.
    ''' You must have called the constructor and authenticate first.
    ''' </summary>
    ''' <returns>WIP. Sometimes returns false if connection was unsucessful.</returns>
    ''' <remarks>Uses the IP, port, username and password from earlier.</remarks>
    Public Function startChatConnection()
        Return chat.init(settings.Username, settings.Password, settings.ServerIP, settings.ServerPort)
    End Function

    ''' <summary>
    ''' Stops an active chat connection.
    ''' </summary>
    Public Sub stopChatConnection()
        chat.Disconnect()
    End Sub

    ''' <summary>
    ''' Just another step in the chat message event chain ;3
    ''' </summary>
    ''' <param name="args"></param>
    ''' <remarks></remarks>
    Private Sub chat_textReceived(args As EventArgs.ChatReceivedEventArgs) Handles chat.textReceived
        RaiseEvent chatMessageReceived(args)
    End Sub
End Class
