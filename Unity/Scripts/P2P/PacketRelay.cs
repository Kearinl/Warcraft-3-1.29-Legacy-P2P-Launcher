using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PacketRelay : MonoBehaviour
{
    [Header("Relay Server")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5048;

    [Header("Player")]
    public string playerId;

    private UdpClient udp;
    private IPEndPoint relayEndPoint;

    void Start()
    {
        udp = new UdpClient(0);

        relayEndPoint = new IPEndPoint(Resolve(serverIP), serverPort);

        if (string.IsNullOrEmpty(playerId))
            playerId = Guid.NewGuid().ToString();

        Debug.Log("Relay ready: " + relayEndPoint);
    }

    // ? FIXED SIGNATURE (MATCHES YOUR SNiffer CALL)
    public void Send(byte[] data, string senderId)
    {
        if (udp == null) return;

        try
        {
            string payload = Convert.ToBase64String(data);

            // format: senderId|base64
            string msg = $"{senderId}|{payload}";

            byte[] bytes = Encoding.UTF8.GetBytes(msg);

            udp.Send(bytes, bytes.Length, relayEndPoint);

            Debug.Log("SENDING TO RELAY: " + relayEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Relay send failed: " + ex.Message);
        }
    }

    private IPAddress Resolve(string host)
    {
        if (IPAddress.TryParse(host, out var ip))
            return ip;

        var list = Dns.GetHostAddresses(host);

        foreach (var a in list)
            if (a.AddressFamily == AddressFamily.InterNetwork)
                return a;

        return list[0];
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}