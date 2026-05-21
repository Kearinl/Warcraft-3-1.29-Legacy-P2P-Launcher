using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WC3Launcher : MonoBehaviour
{
    [Header("Game Path")]
    public string gamePath =
        @"C:\Warcraft III\Frozen Throne.exe";

    public void Launch()
    {
        if (string.IsNullOrEmpty(gamePath))
        {
            Debug.LogError("WC3 path missing");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = gamePath,
                UseShellExecute = true
            });

            Debug.Log("WC3 Launched");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Launch failed: " + e.Message);
        }
    }
}