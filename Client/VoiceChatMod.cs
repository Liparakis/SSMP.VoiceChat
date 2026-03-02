using BepInEx;
using SSMP.Api.Client;
using SSMP.Api.Server;
using SsmpVoiceChat.Server;

namespace SsmpVoiceChat.Client; 

/// <summary>
/// The voice chat mod class.
/// </summary>
[BepInAutoPlugin(id: "io.github.bobbythecatfish.SSMP.VoiceChat", version: Identifier.AddonVersion)]
public partial class VoiceChatMod : BaseUnityPlugin {
    /// <summary>
    /// Statically accessible mod settings.
    /// </summary>
    public static ModSettings ModSettings = new();

    /// <inheritdoc />
    public void Awake() {
        ClientAddon.RegisterAddon(new VoiceChatClientAddon());
        ServerAddon.RegisterAddon(new VoiceChatServerAddon());
        ModSettings = ModSettings.LoadFromFile();
    }

    /// <inheritdoc />
    public void OnLoadGlobal(ModSettings modSettings) {
        ModSettings = modSettings ?? new ModSettings();
    }
}