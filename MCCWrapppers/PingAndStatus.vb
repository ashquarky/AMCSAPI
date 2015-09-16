Imports System.Net.Sockets
Imports System.Collections.Generic
Imports System.Text
Imports System
Imports System.Text.RegularExpressions

''' <summary>
''' More modded MCC code in VB. What am I supposed to do when I can't override?
''' </summary>
''' <remarks></remarks>
Friend Class PingAndStatus
    Implements IDisposable
    Dim c As TcpClient
    Dim encrypted As Boolean = False
    Private Sub New(Client As TcpClient)
        c = Client
    End Sub
    Public Sub New()
    End Sub
    Friend Function ping(host As String, port As UShort) As InfoStructures.ServerInformation
        Return interpretJSON(doPing(host, port, 0))
    End Function
    Private Function doPing(host As String, port As UShort, ByRef protocolversion As Integer) As String
        Dim tcp As New TcpClient(host, port)
        tcp.ReceiveBufferSize = 1024 * 1024
        Dim packet_id As Byte() = getVarInt(0)
        Dim protocol_version As Byte() = getVarInt(4)
        Dim server_adress_val As Byte() = Encoding.UTF8.GetBytes(host)
        Dim server_adress_len As Byte() = getVarInt(server_adress_val.Length)
        Dim server_port As Byte() = BitConverter.GetBytes(CUShort(port))
        Array.Reverse(server_port)
        Dim next_state As Byte() = getVarInt(1)
        Dim packet As Byte() = concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state)
        Dim tosend As Byte() = concatBytes(getVarInt(packet.Length), packet)
        tcp.Client.Send(tosend, SocketFlags.None)
        Dim status_request As Byte() = getVarInt(0)
        Dim request_packet As Byte() = concatBytes(getVarInt(status_request.Length), status_request)
        tcp.Client.Send(request_packet, SocketFlags.None)
        Dim ComTmp As New PingAndStatus(tcp)
        If ComTmp.readNextVarInt() > 0 Then
            If ComTmp.readNextVarInt() = (&H0) Then
                Dim result As String = ComTmp.readNextString() ''This should be JSON stuff
                Return result
            End If
        End If
        Return Nothing
    End Function
    Private Shared Function getVarInt(paramInt As Integer) As Byte()
        Dim bytes As New List(Of Byte)()
        While (paramInt And -128) <> 0
            bytes.Add(CByte(paramInt And 127 Or 128))
            paramInt = CInt(CUInt(paramInt) >> 7)
        End While
        bytes.Add(CByte(paramInt))
        Return bytes.ToArray()
    End Function
    Private Function concatBytes(ParamArray bytes As Byte()()) As Byte()
        Dim result As New List(Of Byte)()
        For Each array As Byte() In bytes
            result.AddRange(array)
        Next
        Return result.ToArray()
    End Function
    Private Function readNextVarInt() As Integer
        Dim i As Integer = 0
        Dim j As Integer = 0
        Dim k As Integer = 0
        Dim tmp As Byte() = New Byte(0) {}
        While True
            Receive(tmp, 0, 1, SocketFlags.None)
            k = tmp(0)
            i = i Or (k And &H7F) << System.Math.Max(System.Threading.Interlocked.Increment(j), j - 1) * 7
            If j > 5 Then
                Throw New OverflowException("VarInt too big - is this really a Minecraft server?")
            End If
            If (k And &H80) <> 128 Then
                Exit While
            End If
        End While
        Return i
    End Function
    Private Sub Receive(ByRef buffer As Byte(), start As Integer, offset As Integer, f As SocketFlags, Optional hackyWorkaround As Boolean = False)
        Dim read As Integer = 0
        While read < offset
            If encrypted Then
                'read += s.Read(buffer, start + read, offset - read)
            Else
                read += c.Client.Receive(buffer, start + read, offset - read, f)
                If hackyWorkaround Then ''Oh yes, I sunk this low to get this to work.
                    Exit Sub
                End If
            End If
        End While
    End Sub
    Private Function readNextString() As String
        Dim length As Integer = readNextVarInt()
        If length > 0 Then
            Dim cache As Byte() = New Byte(length) {}
            Receive(cache, 0, length, SocketFlags.None, True)
            Return Encoding.UTF8.GetString(cache)
        Else
            Return ""
        End If
    End Function

    Private Function interpretJSON(json As String) ''Regex? Oh no.
        Dim out As New InfoStructures.ServerInformation
        Dim svrVersionName As String = Regex.Match(Regex.Match(json, """version"":\s*\{\s*""name"":"".*""\s*,\s*""protocol""").Value, ":"".*"",").Value ''I have no idea how I wrote this.
        out.serverVersionName = svrVersionName.Substring(2, svrVersionName.Length - 4)
        Try
            out.protocolVersion = Regex.Match(json, """protocol"":.*},").Value.Split(":")(1).Trim().Split("}")(0)
            out.setVersionFromProtocol()
        Catch ex As InvalidCastException ''Some absolutely stupid servers respond with things like "-1" or "cool"
            out.protocolVersion = Nothing
            out.serverVersion = Nothing
        End Try
        Try
            out.maxPlayers = Regex.Match(json, """players"":\s*\{\s*""max"":\s*[0-9]+,").Value.Split(":")(2).Trim().Split(",")(0)
        Catch ex As Exception
            out.maxPlayers = Nothing
        End Try
        Try
            out.onlinePlayers = Regex.Match(json, """online"":[0-9]+").Value.Split(":")(1).Trim()
        Catch ex As Exception
            out.onlinePlayers = Nothing
        End Try
        out.descriptionRaw = Regex.Match(json, """description"":""[^""]*""").Value.Split("""")(3)
        out.iconRaw = Regex.Match(json, "base64,[^""]+").Value.Split(",")(1)
        Return out
    End Function

    Friend Sub Dispose() Implements IDisposable.Dispose
        GC.SuppressFinalize(Me)
    End Sub
End Class
