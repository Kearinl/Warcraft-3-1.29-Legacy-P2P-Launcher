using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class RelayServer
{
    static UdpClient server = new UdpClient(5048);

    static ConcurrentDictionary<string, IPEndPoint> peers = new();

    static void Main()
    {
        Console.WriteLine("Relay running on 5048...");
        server.BeginReceive(Receive, null);
        Console.ReadLine();
    }

    static void Receive(IAsyncResult ar)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = server.EndReceive(ar, ref ep);

        string msg = Encoding.UTF8.GetString(data);

        // PING SYSTEM
        if (msg == "ping")
        {
            Console.WriteLine($"PING from {ep.Address} → sending PONG");
            server.Send(Encoding.UTF8.GetBytes("pong"), 4, ep);
            server.BeginReceive(Receive, null);
            return;
        }

        string[] parts = msg.Split('|');

        if (parts.Length != 2)
        {
            server.BeginReceive(Receive, null);
            return;
        }

        string playerId = parts[0];
        byte[] packet = Convert.FromBase64String(parts[1]);

        peers[playerId] = ep;

        Console.WriteLine($"RECV {ep.Address}:{ep.Port} → {packet.Length} bytes");
        Console.WriteLine($"PLAYER {playerId} PACKET ({packet.Length} bytes)");

        foreach (var p in peers)
        {
            if (p.Key == playerId)
                continue;

            server.Send(packet, packet.Length, p.Value);
        }

        server.BeginReceive(Receive, null);
    }
}