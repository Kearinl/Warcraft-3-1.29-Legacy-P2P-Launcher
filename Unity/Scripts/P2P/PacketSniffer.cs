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

    private readonly Queue<byte[]> packetQueue = new();
    private readonly object queueLock = new();

    void Start()
    {
        clientId = Guid.NewGuid().ToString();

        var devices = CaptureDeviceList.Instance;

        Debug.Log("=== WC3 ADAPTER SCAN START ===");

        if (devices.Count == 0)
        {
            Debug.LogError("No capture devices found (Npcap issue)");
            return;
        }

        var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        ICaptureDevice bestDevice = null;
        int bestScore = -1;

        for (int i = 0; i < devices.Count; i++)
        {
            var d = devices[i];
            string desc = d.Description.ToLower();

            Debug.Log($"[ADAPTER {i}] {d.Description}");

            // skip bad adapters
            if (desc.Contains("loopback") ||
                desc.Contains("npcap") ||
                desc.Contains("virtual") ||
                desc.Contains("hyper-v") ||
                desc.Contains("docker") ||
                desc.Contains("vpn"))
            {
                continue;
            }

            int score = 0;

            // base hardware preference
            if (desc.Contains("ethernet")) score += 100;
            if (desc.Contains("wi-fi") || desc.Contains("wireless")) score += 80;
            if (desc.Contains("intel")) score += 40;
            if (desc.Contains("realtek")) score += 40;

            // match OS active interfaces
            foreach (var ni in activeInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                string niName = ni.Name.ToLower();
                string niDesc = ni.Description.ToLower();

                if (desc.Contains(niName) || desc.Contains(niDesc))
                    score += 200;
            }

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

        Debug.Log("=== SELECTED ADAPTER ===");
        Debug.Log(device.Description);

        device.OnPacketArrival += OnPacketArrival;
        device.Open();
        device.StartCapture();

        Debug.Log("PacketSniffer started");
    }

    private void OnPacketArrival(object sender, CaptureEventArgs e)
    {
        try
        {
            var raw = e.Packet;
            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);

            var udp = packet.Extract(typeof(UdpPacket)) as UdpPacket;
            if (udp == null) return;

            int src = udp.SourcePort;
            int dst = udp.DestinationPort;

            // WC3 LAN filter
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
            // ignore
        }
    }

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
            device?.StopCapture();
            device?.Close();
        }
        catch { }
    }
}