using System;

[Serializable]
public class Peer
{
    public string Id;
    public string Ip;
    public int Port;
    public long LastSeenTicks;
}