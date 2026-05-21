using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// ------------------------------------
// THREAD SAFE PEER STORAGE
// ------------------------------------
ConcurrentDictionary<string, Peer> peers =
    new();

// ------------------------------------
// REGISTER / HEARTBEAT
// ------------------------------------
app.MapPost("/register", (Peer peer) =>
{
    peer.LastSeen = DateTime.UtcNow;

    peers[peer.Id] = peer;

    Console.WriteLine(
        $"[{DateTime.Now:T}] " +
        $"REGISTER {peer.Id} " +
        $"@ {peer.Ip}:{peer.Port}"
    );

    return Results.Ok();
});

// ------------------------------------
// GET ONLINE PEERS
// ------------------------------------
app.MapGet("/peers", () =>
{
    CleanupPeers(peers);

    return peers.Values.ToList();
});

// ------------------------------------
// SERVER STATUS
// ------------------------------------
app.MapGet("/", () =>
{
    CleanupPeers(peers);

    return Results.Ok(new
    {
        status = "online",
        peers = peers.Count
    });
});

// ------------------------------------
// CLEANUP OFFLINE PEERS
// ------------------------------------
void CleanupPeers(
    ConcurrentDictionary<string, Peer> peerList
)
{
    var now = DateTime.UtcNow;

    foreach (var peer in peerList.Values)
    {
        if ((now - peer.LastSeen)
            .TotalSeconds > 30)
        {
            peerList.TryRemove(
                peer.Id,
                out _
            );

            Console.WriteLine(
                $"[{DateTime.Now:T}] " +
                $"REMOVED {peer.Id}"
            );
        }
    }
}

app.Run("http://0.0.0.0:5047");

// ------------------------------------
// PEER MODEL
// ------------------------------------
public class Peer
{
    public string Id { get; set; } = "";

    public string Ip { get; set; } = "";

    public int Port { get; set; }

    public DateTime LastSeen { get; set; }
}