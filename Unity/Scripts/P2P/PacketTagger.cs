using System;
using System.Text;

public static class PacketTagger
{
    // creates a "soft session id" based on host machine + time
    public static string SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);

    public static int Sequence = 0;

    public static string Wrap(byte[] data, string senderId)
    {
        Sequence++;

        string payload = Convert.ToBase64String(data);

        // FORMAT:
        // session|sender|seq|payload
        return $"{SessionId}|{senderId}|{Sequence}|{payload}";
    }

    public static bool TryUnwrap(string msg, out string sender, out int seq, out byte[] data)
    {
        sender = "";
        seq = 0;
        data = null;

        var parts = msg.Split('|');
        if (parts.Length != 4)
            return false;

        sender = parts[1];
        seq = int.Parse(parts[2]);
        data = Convert.FromBase64String(parts[3]);

        return true;
    }
}