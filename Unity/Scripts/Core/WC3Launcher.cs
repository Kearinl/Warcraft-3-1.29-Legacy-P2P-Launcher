using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WC3Launcher : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField pathInput;
    public TMP_Text statusText;

    [Header("Default Path")]
    public string defaultPath =
        @"C:\Warcraft III\Frozen Throne.exe";

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip saveClip;
    public AudioClip launchClip;
    public AudioClip errorClip;

    private const string SaveKey = "WC3_PATH";
    private string lastValidPath;

    private System.Diagnostics.Process wc3Process;

    void Start()
    {
        string saved = PlayerPrefs.GetString(SaveKey, "");

        if (string.IsNullOrEmpty(saved))
            saved = defaultPath;

        pathInput.text = saved;

        lastValidPath = File.Exists(saved) ? saved : "";
        ValidateAndMaybeSave(saved);
    }

    // Called when user edits TMP input field
    public void OnPathChanged()
    {
        ValidateAndMaybeSave(pathInput.text);
    }

    // 🔍 Auto-detect WC3 install
    public void AutoFindWC3()
    {
        string[] commonPaths =
        {
            @"C:\Program Files (x86)\Warcraft III\Frozen Throne.exe",
            @"C:\Program Files\Warcraft III\Frozen Throne.exe",
            @"C:\Warcraft III\Frozen Throne.exe",
            @"D:\Warcraft III\Frozen Throne.exe",
            @"D:\WoW\Warcraft III (Legacy)\Warcraft III.exe"
        };

        foreach (string path in commonPaths)
        {
            if (File.Exists(path))
            {
                pathInput.text = path;
                ValidateAndMaybeSave(path);
                SetStatus("Warcraft III found and saved", true);
                PlaySound(saveClip);
                return;
            }
        }

        SetStatus("WC3 not found. Please paste your install path.", false);
        PlaySound(errorClip);
    }

    // Core validation + conditional save
    private void ValidateAndMaybeSave(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            SetStatus("Please enter Warcraft III path", false);
            PlaySound(errorClip);
            return;
        }

        if (!File.Exists(path))
        {
            SetStatus("Invalid Warcraft III install path", false);
            PlaySound(errorClip);
            return;
        }

        SetStatus("Valid install detected", true);

        if (path != lastValidPath)
        {
            PlayerPrefs.SetString(SaveKey, path);
            PlayerPrefs.Save();

            lastValidPath = path;

            SetStatus("Valid install saved", true);
            PlaySound(saveClip);
        }
    }

    // 🎮 Launch WC3 (play sound FIRST, then mute + start)
    public void Launch()
    {
        string path = pathInput.text;

        if (!File.Exists(path))
        {
            SetStatus("Cannot launch: invalid path", false);
            PlaySound(errorClip);
            return;
        }

        // 🎵 Play launch sound BEFORE muting
        PlaySound(launchClip);

        SetStatus("Launching Warcraft III...", true);

        Invoke(nameof(DoLaunch), 0.2f);
    }

    private void DoLaunch()
    {
        string path = pathInput.text;

        // 🔇 MUTE launcher audio AFTER sound plays
        AudioListener.volume = 0f;

        PlayerPrefs.SetString(SaveKey, path);
        PlayerPrefs.Save();

        try
        {
            wc3Process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                }
            );
        }
        catch (System.Exception e)
        {
            AudioListener.volume = 1f;

            SetStatus("Launch failed: " + e.Message, false);
            PlaySound(errorClip);
            Debug.LogError(e);
        }
    }

    // 🔄 Restore audio when WC3 closes
    void Update()
    {
        if (wc3Process != null && wc3Process.HasExited)
        {
            wc3Process = null;

            AudioListener.volume = 1f;
            SetStatus("Warcraft III closed", true);
        }
    }

    // 🔊 Sound helper
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SetStatus(string msg, bool ok)
    {
        if (statusText == null) return;

        statusText.text = msg;
        statusText.color = ok ? Color.green : Color.red;
    }
}