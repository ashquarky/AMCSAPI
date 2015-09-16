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
    Friend Function doPing(host As String, port As UShort, ByRef protocolversion As Integer) As String
        Console.WriteLine(host)
        Console.WriteLine("Setting up...")
        Dim tcp As New TcpClient(host, port)
        tcp.ReceiveBufferSize = 1024 * 1024
        Console.WriteLine("TCPclient OK")
        Dim packet_id As Byte() = getVarInt(0)
        Console.WriteLine("packetID OK")
        Dim protocol_version As Byte() = getVarInt(4)
        Console.WriteLine("protocol version OK")
        Dim server_adress_val As Byte() = Encoding.UTF8.GetBytes(host)
        Console.WriteLine("server_address_val OK")
        Dim server_adress_len As Byte() = getVarInt(server_adress_val.Length)
        Console.WriteLine("sevrer_address_len OK")
        Dim server_port As Byte() = BitConverter.GetBytes(CUShort(port))
        Console.WriteLine("server port OK")
        Array.Reverse(server_port)
        Console.WriteLine("reversed server port OK Data:" + System.Text.Encoding.BigEndianUnicode.GetString(server_port))
        Dim next_state As Byte() = getVarInt(1)
        Console.WriteLine("next_state OK")
        Dim packet As Byte() = concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state)
        Console.WriteLine("packet OK")
        Dim tosend As Byte() = concatBytes(getVarInt(packet.Length), packet)
        Console.WriteLine("compressed packet OK")
        tcp.Client.Send(tosend, SocketFlags.None)
        Console.WriteLine("packet sent. setting up status_request...")
        Dim status_request As Byte() = getVarInt(0)
        Console.WriteLine("status_request OK")
        Dim request_packet As Byte() = concatBytes(getVarInt(status_request.Length), status_request)
        Console.WriteLine("compressed packet OK")
        tcp.Client.Send(request_packet, SocketFlags.None)
        Console.WriteLine("packet sent. getting response...")
        Dim ComTmp As New PingAndStatus(tcp)
        Console.WriteLine("ComTmp OK")
        If ComTmp.readNextVarInt() > 0 Then
            Console.WriteLine("readNextVarInt OK")
            If ComTmp.readNextVarInt() = (&H0) Then
                Console.WriteLine("readNextVarInt2 OK")
                Dim result As String = ComTmp.readNextString() ''This should be JSON stuff

                Return result
            End If
        End If
        Return Nothing
    End Function
    Private Shared Function getVarInt(paramInt As Integer) As Byte()
        Console.WriteLine("[SvrIntr] getting varint")
        Dim bytes As New List(Of Byte)()
        Console.WriteLine("[SvrIntr] adding bytes")
        While (paramInt And -128) <> 0
            bytes.Add(CByte(paramInt And 127 Or 128))
            paramInt = CInt(CUInt(paramInt) >> 7)
        End While
        bytes.Add(CByte(paramInt))
        Console.WriteLine("[SvrIntr] done; returning")
        Return bytes.ToArray()
    End Function
    Private Function concatBytes(ParamArray bytes As Byte()()) As Byte()
        Console.WriteLine("[SvrIntr] concatting bytes...")
        Dim result As New List(Of Byte)()
        For Each array As Byte() In bytes
            result.AddRange(array)
        Next
        Console.WriteLine("[SvrIntr] done; returning")
        Return result.ToArray()
    End Function
    Private Function readNextVarInt() As Integer
        Console.WriteLine("[SvrIntr] getting next varint")
        Dim i As Integer = 0
        Dim j As Integer = 0
        Dim k As Integer = 0
        Dim tmp As Byte() = New Byte(0) {}
        Console.WriteLine("[SvrIntr] receiving")
        While True
            Receive(tmp, 0, 1, SocketFlags.None)
            Console.WriteLine("[SvrIntr] received something")
            k = tmp(0)
            i = i Or (k And &H7F) << System.Math.Max(System.Threading.Interlocked.Increment(j), j - 1) * 7
            If j > 5 Then
                Throw New OverflowException("VarInt too big")
            End If
            Console.WriteLine("[SvrIntr] math all good")
            If (k And &H80) <> 128 Then
                Console.WriteLine("[SvrIntr] passed validation; returning")
                Exit While
            End If
            Console.WriteLine("[SvrIntr] restarting...")
        End While
        Return i
    End Function
    Private Sub Receive(ByRef buffer As Byte(), start As Integer, offset As Integer, f As SocketFlags, Optional hackyWorkaround As Boolean = False)
        Dim read As Integer = 0
        Console.WriteLine("[READ] reading...")
        While read < offset
            If encrypted Then
                ''read += s.Read(buffer, start + read, offset - read)
            Else
                Console.WriteLine("[READ] noencryption")
                read += c.Client.Receive(buffer, start + read, offset - read, f)
                Console.WriteLine("[READ] " + read.ToString())
                If hackyWorkaround Then ''Oh yes, I sunk this low to get this to work.
                    Exit Sub
                End If
            End If
        End While
        Console.WriteLine("[READ] done; returning")
    End Sub
    Private Function readNextString() As String
        Console.WriteLine("[SvrIntr] getting next string")
        Dim length As Integer = readNextVarInt()
        Console.WriteLine("[SvrIntr] gotvarint; starting mathsy stuff")
        If length > 0 Then
            Dim cache As Byte() = New Byte(length) {}
            Console.WriteLine("[SvrIntr] receiving...")
            Receive(cache, 0, length, SocketFlags.None, True)
            Console.WriteLine("[SvrIntr] all good; returning")
            Return Encoding.UTF8.GetString(cache)
        Else
            Console.WriteLine("[SvrIntr] failed")
            Return ""
        End If
    End Function

    Private Function interpretJSON(json As String) ''Regex? Oh no.
        Dim out As New InfoStructures.ServerInformation
        Dim svrVersionName As String = Regex.Match(Regex.Match(json, """version"":\s*\{\s*""name"":"".*""\s*,\s*""protocol""").Value, ":"".*"",").Value ''I have no idea how I wrote this.
        out.serverVersionName = svrVersionName.Substring(2, svrVersionName.Length - 4)
        Console.WriteLine(out.serverVersionName)
        Try
            out.protocolVersion = Regex.Match(json, """protocol"":.*},").Value.Split(":")(1).Trim().Split("}")(0)
            out.setVersionFromProtocol()
        Catch ex As InvalidCastException ''Some absolutely stupid servers respond with things like "-1" or "cool"
            out.protocolVersion = Nothing
            out.serverVersion = Nothing
        End Try
        Console.WriteLine(out.protocolVersion)
        Console.WriteLine(out.serverVersion)
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
        Console.WriteLine(out.maxPlayers)
        Console.WriteLine(out.onlinePlayers)
        Return out
    End Function
End Class
