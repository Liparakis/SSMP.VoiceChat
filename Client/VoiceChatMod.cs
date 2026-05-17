using BepInEx;
using OpenTK.Audio.OpenAL;
using SSMP.Api.Client;
using SSMP.Api.Server;
using SsmpVoiceChat.Server;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SsmpVoiceChat.Client;

/// <summary>
/// The voice chat mod class.
/// </summary>
[BepInAutoPlugin(id: "io.github.bobbythecatfish.SSMP.VoiceChat", version: Identifier.AddonVersion)]
public partial class VoiceChatMod : BaseUnityPlugin
{
    /// <summary>
    /// Statically accessible mod settings.
    /// </summary>
    internal static ModSettings ModSettings;

    internal static IChatBox ChatBox;
    internal static bool ToggleMuted = false;

    const string url = "https://www.openal.org/downloads";

    /// <inheritdoc />
    public void Awake()
    {
        // Catch if the player doesn't have OpenAL installed
        try
        {
            Alc.GetError(IntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
#if DEBUG == false
            try
            {
                // Open download link in browser
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
            Logger.LogError($"OpenAL not installed. Please install at {url}");
            SceneManager.sceneLoaded += OpenALErrorWarning;
        }
#endif

            ClientAddon.RegisterAddon(new VoiceChatClientAddon());
            ServerAddon.RegisterAddon(new VoiceChatServerAddon());
            ModSettings = new ModSettings(Config);
        }

        void Update()
        {
            if (ModSettings.InputMode == ModSettings.InputMethod.PushToToggle)
            {
                if (Input.GetKeyDown(ModSettings.PushToTalkKey))
                {
                    ToggleMuted = !ToggleMuted;
                }
            }
        }

        void OpenALErrorWarning(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu_Title") return;
            SceneManager.sceneLoaded -= OpenALErrorWarning;

            // Create copy of one of the buttons
            var canvas = UIManager.instance.UICanvas.transform;
            var example = canvas.Find("MainMenuScreen").GetChild(0).GetChild(0).GetChild(0);
            var textGO = GameObject.Instantiate(example, canvas);

            // Remove components
            if (textGO.TryGetComponent<ContentSizeFitter>(out var fitter))
            {
                Component.DestroyImmediate(fitter);
            }

            if (textGO.TryGetComponent<FixVerticalAlign>(out var align))
            {
                Component.DestroyImmediate(align);
            }

            // Set position and size
            var rect = textGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1055, 300);
            textGO.SetLocalPosition2D(0, 190);

            var parent = new GameObject("Warning Background");
            parent.transform.parent = canvas;
            parent.transform.position = textGO.position;
            parent.transform.SetScale2D(Vector2.one);
            rect = parent.AddComponentIfNotPresent<RectTransform>();
            rect.sizeDelta = new Vector2(1055, 300);

            var image = parent.AddComponent<Image>();
            image.color = new Color(.34f, .34f, .34f, 0.8f);

            textGO.SetParentReset(parent.transform);
            textGO.SetPositionZ(-15);

            // Set text
            var text = textGO.GetComponent<Text>();
            text.text = $"You need to install OpenAL for SSMP Voice Chat to work. Download at OpenAL.org";
            text.lineSpacing = 1;
        }
    }
}