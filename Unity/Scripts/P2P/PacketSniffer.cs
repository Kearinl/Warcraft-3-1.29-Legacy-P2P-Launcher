using UnityEngine;
using SharpPcap;
using PacketDotNet;
using System;
using System.Linq;

public class PacketSniffer : MonoBehaviour
{
    [Header("Relay")]
    public PacketRelay relay;

    [Header("Debug")]
    public bool logPackets = true;

    private ICaptureDevice device;
    private string clientId;

    void Start()
    {
        clientId = Guid.NewGuid().ToString();

        var devices = CaptureDeviceList.Instance;

        if (devices.Count == 0)
        {
            Debug.LogError(
                "No capture devices found."
            );

            return;
        }

        // ------------------------------------
        // SMART DEVICE SELECTION
        // ------------------------------------

        foreach (var d in devices)
        {
            string desc =
                d.Description.ToLower();

            Debug.Log(
                "Found Adapter: " +
                d.Description
            );

            // SKIP BAD ADAPTERS
            if (desc.Contains("miniport") ||
                desc.Contains("loopback") ||
                desc.Contains("npcap") ||
                desc.Contains("vpn") ||
                desc.Contains("virtual") ||
                desc.Contains("hyper-v") ||
                desc.Contains("docker"))
            {
                continue;
            }

            // PREFER REAL LAN/WIFI
            if (desc.Contains("ethernet") ||
                desc.Contains("wi-fi") ||
                desc.Contains("wireless") ||
                desc.Contains("realtek") ||
                desc.Contains("intel"))
            {
                device = d;
                break;
            }
        }

        // FALLBACK
        if (device == null)
        {
            device = devices[0];

            Debug.LogWarning(
                "Fallback adapter selected: " +
                device.Description
            );
        }

        Debug.Log(
            "USING ADAPTER: " +
            device.Description
        );

        // ------------------------------------
        // START CAPTURE
        // ------------------------------------

        device.OnPacketArrival +=
            OnPacketArrival;

        device.Open();

        device.StartCapture();

        Debug.Log(
            "PacketSniffer started"
        );
    }

    // ------------------------------------
    // PACKET EVENT
    // ------------------------------------
    private void OnPacketArrival(
        object sender,
        CaptureEventArgs e
    )
    {
        try
        {
            var raw = e.Packet;

            Packet packet =
                Packet.ParsePacket(
                    raw.LinkLayerType,
                    raw.Data
                );

            UdpPacket udp =
                (UdpPacket)packet.Extract(
                    typeof(UdpPacket)
                );

            if (udp == null)
                return;

            int src = udp.SourcePort;
            int dst = udp.DestinationPort;

            // WC3 LAN PORT
            if (src != 6112 &&
                dst != 6112)
                return;

            byte[] payload =
                udp.PayloadData;

            if (payload == null ||
                payload.Length == 0)
                return;

            if (logPackets)
            {
                Debug.Log(
                    "WC3 UDP: " +
                    payload.Length +
                    " bytes"
                );
            }

            if (relay != null)
            {
                relay.Send(
                    payload,
                    clientId
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                ex.Message
            );
        }
    }

    // ------------------------------------
    // CLEANUP
    // ------------------------------------
    void OnDestroy()
    {
        try
        {
            if (device != null)
            {
                device.StopCapture();
                device.Close();
            }
        }
        catch { }
    }
}