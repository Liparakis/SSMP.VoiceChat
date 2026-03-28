using BepInEx;
using OpenTK.Audio.OpenAL;
using SSMP.Api.Client;
using SSMP.Api.Server;
using SsmpVoiceChat.Server;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SsmpVoiceChat.Client; 

/// <summary>
/// The voice chat mod class.
/// </summary>
[BepInAutoPlugin(id: "io.github.bobbythecatfish.SSMP.VoiceChat", version: Identifier.AddonVersion)]
public partial class VoiceChatMod : BaseUnityPlugin {
    /// <summary>
    /// Statically accessible mod settings.
    /// </summary>
    internal static ModSettings ModSettings;
    internal static IChatBox ChatBox;

    const string url = "https://www.openal.org/downloads";
    bool errored = false;

    /// <inheritdoc />
    public void Awake() {
        try
        {
            Alc.GetError(IntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
            //Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Logger.LogError($"OpenAL not installed. Please install at {url}");
            errored = true;
        }

        SceneManager.sceneLoaded += OpenALErrorWarning;
        ClientAddon.RegisterAddon(new VoiceChatClientAddon());
        ServerAddon.RegisterAddon(new VoiceChatServerAddon());
        ModSettings = new ModSettings(Config);
    }

    void OpenALErrorWarning(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu_Title") return;
        SceneManager.sceneLoaded -= OpenALErrorWarning;

        Logger.LogWarning(errored);
        if (!errored) return;

        var canvas = UIManager.instance.UICanvas.transform;
        var example = canvas.Find("MainMenuScreen").GetChild(0).GetChild(0).GetChild(0);
        var textGO = GameObject.Instantiate(example, canvas);
        if (textGO.TryGetComponent<ContentSizeFitter>(out var fitter))
        {
            Component.DestroyImmediate(fitter);
        }

        if (textGO.TryGetComponent<FixVerticalAlign>(out var align))
        {
            Component.DestroyImmediate(align);
        }

        var rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1055, 300);
        textGO.SetLocalPosition2D(0, 190);

        var text = textGO.GetComponent<Text>();
        text.text = $"You need to install OpenAL for SSMP Voice Chat to work. Download at OpenAL.org";
        text.lineSpacing = 1;


        var title = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "LogoTitle");
        if (title != null)
        {
            var renderer = title.GetComponent<SpriteRenderer>();
            renderer.enabled = false;
        }
    }
}