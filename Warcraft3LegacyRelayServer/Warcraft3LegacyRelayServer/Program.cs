using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static UdpClient server = new UdpClient(5048);

    // senderId -> endpoint
    static ConcurrentDictionary<string, IPEndPoint> peers = new();

    // duplicate + replay protection
    static ConcurrentDictionary<string, DateTime> seenPackets = new();

    static void Main()
    {
        Console.Title = "WC3 Stable Relay Server";
        Console.WriteLine("WC3 Stable Relay running on port 5048...");
        Console.WriteLine("Waiting for packets...\n");

        server.BeginReceive(Receive, null);

        // keep alive
        while (true)
        {
            System.Threading.Thread.Sleep(1000);
            Cleanup();
        }
    }

    static void Receive(IAsyncResult ar)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

        byte[] data;

        try
        {
            data = server.EndReceive(ar, ref ep);
        }
        catch
        {
            server.BeginReceive(Receive, null);
            return;
        }

        string msg = Encoding.UTF8.GetString(data);

        // ---------------------------
        // PING / PONG (debug)
        // ---------------------------
        if (msg == "ping")
        {
            Console.WriteLine($"[PING] {ep.Address} → PONG");
            server.Send(Encoding.UTF8.GetBytes("pong"), 4, ep);
            server.BeginReceive(Receive, null);
            return;
        }

        // ---------------------------
        // EXPECTED FORMAT:
        // session|sender|seq|base64data
        // ---------------------------
        string[] parts = msg.Split('|');

        if (parts.Length != 4)
        {
            server.BeginReceive(Receive, null);
            return;
        }

        string session = parts[0];
        string sender = parts[1];

        if (!int.TryParse(parts[2], out int seq))
        {
            server.BeginReceive(Receive, null);
            return;
        }

        byte[] payload;

        try
        {
            payload = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            server.BeginReceive(Receive, null);
            return;
        }

        string packetKey = $"{sender}:{seq}";

        // ---------------------------
        // DUPLICATE DROP (CRITICAL)
        // ---------------------------
        if (seenPackets.ContainsKey(packetKey))
        {
            server.BeginReceive(Receive, null);
            return;
        }

        seenPackets[packetKey] = DateTime.UtcNow;

        // register sender endpoint
        peers[sender] = ep;

        Console.WriteLine(
            $"[{DateTime.Now:T}] " +
            $"SESSION {session} | " +
            $"PLAYER {sender} | " +
            $"SEQ {seq} | " +
            $"{payload.Length} bytes"
        );

        // ---------------------------
        // FORWARD TO ALL OTHER PEERS
        // ---------------------------
        foreach (var p in peers)
        {
            if (p.Key == sender)
                continue;

            try
            {
                server.Send(payload, payload.Length, p.Value);
            }
            catch
            {
                // ignore broken endpoints
            }
        }

        server.BeginReceive(Receive, null);
    }

    // ---------------------------
    // CLEAN OLD PACKETS
    // ---------------------------
    static void Cleanup()
    {
        var now = DateTime.UtcNow;

        foreach (var kv in seenPackets)
        {
            if ((now - kv.Value).TotalSeconds > 30)
            {
                seenPackets.TryRemove(kv.Key, out _);
            }
        }
    }
}