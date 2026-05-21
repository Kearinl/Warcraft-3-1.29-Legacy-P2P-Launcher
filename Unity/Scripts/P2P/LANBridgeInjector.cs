using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class LANBridgeInjector : MonoBehaviour
{
    public string remoteIP = "127.0.0.1";

    public int listenPort = 6200;

    private UdpClient listener;

    void Start()
    {
        listener =
            new UdpClient(listenPort);

        listener.BeginReceive(
            OnReceive,
            null
        );

        Debug.Log(
            "Injector listening: " +
            listenPort
        );
    }

    void OnReceive(System.IAsyncResult ar)
    {
        IPEndPoint ep =
            new IPEndPoint(
                IPAddress.Any,
                listenPort
            );

        byte[] data =
            listener.EndReceive(ar, ref ep);

        string msg =
            Encoding.UTF8.GetString(data);

        // SPLIT MESSAGE
        string[] parts =
            msg.Split('|');

        if (parts.Length != 2)
            return;

        byte[] packet =
            System.Convert.FromBase64String(
                parts[1]
            );

        // REBROADCAST TO LOCAL WC3 LAN
        UdpClient rebroadcast =
            new UdpClient();

        rebroadcast.EnableBroadcast = true;

        rebroadcast.Send(
            packet,
            packet.Length,
            new IPEndPoint(
                IPAddress.Broadcast,
                6112
            )
        );

        Debug.Log(
            "Injected WC3 packet locally"
        );

        listener.BeginReceive(
            OnReceive,
            null
        );
    }

    void OnDestroy()
    {
        listener?.Close();
    }
}