using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class MasterServerClient : MonoBehaviour
{
    [Header("Server")]
    public string baseUrl =
        "http://localhost:5047";

    [Header("UI")]
    public TMP_Text statusText;
    public TMP_Text peerCountText;

    [Header("Heartbeat")]
    public float heartbeatInterval = 10f;

    [Header("P2P")]
    public LANBridgeInjector injector;

    private string playerId;
    private string status = "Offline";
    public PacketRelay relay;

    // ------------------------------------
    // START
    // ------------------------------------
    void Start()
    {
        playerId =
            System.Guid.NewGuid().ToString();

        StartCoroutine(
            HeartbeatLoop()
        );
    }

    // ------------------------------------
    // HEARTBEAT LOOP
    // ------------------------------------
    IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            yield return StartCoroutine(
                RegisterRoutine()
            );

            yield return StartCoroutine(
                GetPeersRoutine()
            );

            yield return new WaitForSeconds(
                heartbeatInterval
            );
        }
    }

    // ------------------------------------
    // REGISTER TO MASTER SERVER
    // ------------------------------------
    IEnumerator RegisterRoutine()
    {
        Peer peer = new Peer
        {
            Id = playerId,
            Ip = GetLocalIP(),
            Port = 6200,
            LastSeenTicks =
                System.DateTime.UtcNow.Ticks
        };

        string json =
            JsonUtility.ToJson(peer);

        UnityWebRequest req =
            new UnityWebRequest(
                baseUrl + "/register",
                "POST"
            );

        req.uploadHandler =
            new UploadHandlerRaw(
                System.Text.Encoding.UTF8
                    .GetBytes(json)
            );

        req.downloadHandler =
            new DownloadHandlerBuffer();

        req.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return req.SendWebRequest();

        if (req.result ==
            UnityWebRequest.Result.Success)
        {
            status = "Online";
        }
        else
        {
            status = "Offline";

            Debug.LogWarning(
                "Register failed: " +
                req.error
            );
        }

        UpdateUI();
    }

    // ------------------------------------
    // GET ONLINE PEERS
    // ------------------------------------
    IEnumerator GetPeersRoutine()
    {
        UnityWebRequest req =
            UnityWebRequest.Get(
                baseUrl + "/peers"
            );

        yield return req.SendWebRequest();

        if (req.result !=
            UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                "GetPeers failed: " +
                req.error
            );

            yield break;
        }

        Peer[] peers =
            JsonHelper.FromJson<Peer>(
                req.downloadHandler.text
            );

        int onlinePlayers = peers.Length;

        // REMOVE SELF FROM COUNT
        foreach (var p in peers)
        {
            if (p.Id == playerId)
            {
                onlinePlayers--;
                break;
            }
        }

        // UI
        if (peerCountText != null)
        {
            peerCountText.text =
                "Players Online: " +
                Mathf.Max(onlinePlayers, 0);
        }

        // CONNECT TO FIRST REMOTE PEER
        foreach (var p in peers)
        {
            if (p.Id == playerId)
                continue;

            if (injector != null)
            {
                injector.remoteIP = p.Ip;

                Debug.Log(
                    "Connected to peer: " +
                    p.Ip
                );
            }

            break;
        }
    }

    // ------------------------------------
    // UPDATE UI
    // ------------------------------------
    void UpdateUI()
{
    bool masterOnline = (status == "Online");
    bool relayOnline = relay != null;

    bool fullyOnline = masterOnline && relayOnline;

    if (statusText != null)
    {
        statusText.text =
            fullyOnline
            ? "SYSTEM: ONLINE"
            : "SYSTEM: OFFLINE";
    }
}

    // ------------------------------------
    // GET LOCAL IPV4
    // ------------------------------------
    string GetLocalIP()
    {
        var host =
            System.Net.Dns.GetHostEntry(
                System.Net.Dns.GetHostName()
            );

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily ==
                System.Net.Sockets
                    .AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "127.0.0.1";
    }
}