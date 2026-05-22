using UnityEngine;
using SharpPcap;
using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

public class PacketSniffer : MonoBehaviour
{
    [Header("Relay")]
    public PacketRelay relay;

    [Header("Debug")]
    public bool logPackets = true;

    private ICaptureDevice device;
    private string clientId;

    // thread-safe queue
    private readonly Queue<byte[]> packetQueue = new Queue<byte[]>();
    private readonly object queueLock = new object();

    void Start()
    {
        clientId = Guid.NewGuid().ToString();

        var devices = CaptureDeviceList.Instance;

        if (devices.Count == 0)
        {
            Debug.LogError("No capture devices found.");
            return;
        }

        ICaptureDevice bestDevice = null;
        int bestScore = -1;

        var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var d in devices)
        {
            string desc = d.Description.ToLower();

            Debug.Log("Found Adapter: " + d.Description);

            // skip broken/virtual adapters
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

            int score = 0;

            // prefer physical adapters
            if (desc.Contains("ethernet")) score += 80;
            if (desc.Contains("wi-fi") || desc.Contains("wireless")) score += 60;
            if (desc.Contains("intel")) score += 40;
            if (desc.Contains("realtek")) score += 40;

            // strongly prefer ACTIVE system interface match
            foreach (var ni in activeInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                string niName = ni.Name.ToLower();
                string niDesc = ni.Description.ToLower();

                if (desc.Contains(niName) || desc.Contains(niDesc))
                {
                    score += 200;
                }
            }

            // test if device can actually open
            try
            {
                d.Open();
                d.Close();
            }
            catch
            {
                continue;
            }

            score += 10;

            if (score > bestScore)
            {
                bestScore = score;
                bestDevice = d;
            }
        }

        if (bestDevice == null)
        {
            bestDevice = devices[0];
            Debug.LogWarning("Fallback adapter used: " + bestDevice.Description);
        }

        device = bestDevice;

        Debug.Log("USING ADAPTER: " + device.Description);

        device.OnPacketArrival += OnPacketArrival;

        device.Open();
        device.StartCapture();

        Debug.Log("PacketSniffer started");
    }

    // ============================
    // THREAD SAFE PACKET CAPTURE
    // ============================
    private void OnPacketArrival(object sender, CaptureEventArgs e)
    {
        try
        {
            var raw = e.Packet;

            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);

            var udp = packet.Extract(typeof(UdpPacket)) as UdpPacket;

            if (udp == null)
                return;

            int src = udp.SourcePort;
            int dst = udp.DestinationPort;

            // WC3 LAN port filter
            if (src != 6112 && dst != 6112)
                return;

            byte[] payload = udp.PayloadData;

            if (payload == null || payload.Length == 0)
                return;

            lock (queueLock)
            {
                packetQueue.Enqueue(payload);
            }
        }
        catch
        {
            // ignore capture errors
        }
    }

    // ============================
    // MAIN THREAD PROCESSING
    // ============================
    void Update()
    {
        while (true)
        {
            byte[] payload = null;

            lock (queueLock)
            {
                if (packetQueue.Count > 0)
                    payload = packetQueue.Dequeue();
            }

            if (payload == null)
                break;

            if (logPackets)
                Debug.Log("WC3 UDP: " + payload.Length + " bytes");

            relay?.Send(payload, clientId);
        }
    }

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