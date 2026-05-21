using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System;

public class PacketRelay : MonoBehaviour
{
    [Header("Remote Peer")]
    public string remoteIP = "127.0.0.1";
    public int remotePort = 6200;

    private UdpClient udp;
    private IPEndPoint remoteEndPoint;

    void Start()
    {
        udp = new UdpClient(0);

        remoteEndPoint =
            new IPEndPoint(
                IPAddress.Parse(remoteIP),
                remotePort
            );

        Debug.Log("PacketRelay ready");
    }

    // SEND WC3 PACKET TO PEER
    public void Send(byte[] data, string senderId)
    {
        if (udp == null) return;

        string payload =
            Convert.ToBase64String(data);

        string json =
            $"{senderId}|{payload}";

        byte[] bytes =
            Encoding.UTF8.GetBytes(json);

        udp.Send(
            bytes,
            bytes.Length,
            remoteEndPoint
        );
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}