using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class LANBridgeInjector : MonoBehaviour
{
    [Header("Target (IP or DNS)")]
    public string remoteIP = "127.0.0.1";

    [Header("Listen Port")]
    public int listenPort = 6200;

    [Header("WC3 LAN Port")]
    public int wc3Port = 6112;

    private UdpClient listener;

    // cached resolved IP
    private IPAddress cachedTargetIP;
    private float lastResolveTime;
    public float resolveInterval = 30f;

    void Start()
    {
        listener = new UdpClient(listenPort);
        listener.BeginReceive(OnReceive, null);

        Debug.Log("Injector listening on port: " + listenPort);
    }

    void OnReceive(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);

            byte[] data = listener.EndReceive(ar, ref ep);
            string msg = Encoding.UTF8.GetString(data);

            string[] parts = msg.Split('|');
            if (parts.Length != 2)
            {
                listener.BeginReceive(OnReceive, null);
                return;
            }

            byte[] packet = Convert.FromBase64String(parts[1]);

            // Get resolved target (DNS or IP)
            IPAddress target = GetResolvedIP();

            if (target == null)
            {
                Debug.LogWarning("Failed to resolve target IP: " + remoteIP);
                listener.BeginReceive(OnReceive, null);
                return;
            }

            using (UdpClient sender = new UdpClient())
            {
                sender.Send(packet, packet.Length, new IPEndPoint(target, wc3Port));
            }

            Debug.Log("Injected WC3 packet to: " + target + ":" + wc3Port);
        }
        catch (Exception ex)
        {
            Debug.LogError("Receive error: " + ex.Message);
        }
        finally
        {
            // restart listener
            listener.BeginReceive(OnReceive, null);
        }
    }

    private IPAddress GetResolvedIP()
    {
        // refresh cache every X seconds
        if (cachedTargetIP != null &&
            Time.time - lastResolveTime < resolveInterval)
        {
            return cachedTargetIP;
        }

        cachedTargetIP = ResolveIP(remoteIP);
        lastResolveTime = Time.time;

        return cachedTargetIP;
    }

    private IPAddress ResolveIP(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        // Already IP?
        if (IPAddress.TryParse(host, out IPAddress ip))
            return ip;

        try
        {
            var addresses = Dns.GetHostAddresses(host);

            foreach (var addr in addresses)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                    return addr; // prefer IPv4
            }

            return addresses.Length > 0 ? addresses[0] : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("DNS resolve failed: " + ex.Message);
            return null;
        }
    }

    void OnDestroy()
    {
        listener?.Close();
    }
}