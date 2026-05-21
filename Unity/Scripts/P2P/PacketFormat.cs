using System;

[Serializable]
public class PacketFormat
{
    public string senderId;
    public byte[] data;
    public long timestamp;
}