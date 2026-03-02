using BepInEx;
using Modding;
using SSMP.Api.Client;
using SSMP.Api.Server;
using SsmpVoiceChat.Server;

namespace SsmpVoiceChat.Client; 

/// <summary>
/// The voice chat mod class.
/// </summary>
[BepInAutoPlugin(id: "io.github.bobbythecatfish.SSMP.VoiceChat")]
public partial class VoiceChatMod : BaseUnityPlugin {
    /// <summary>
    /// Statically accessible mod settings.
    /// </summary>
    public static ModSettings ModSettings = new();

    /// <inheritdoc />
    public override string GetVersion() {
        return Identifier.AddonVersion;
    }

    /// <inheritdoc />
    public override void Initialize() {
        ClientAddon.RegisterAddon(new VoiceChatClientAddon());
        ServerAddon.RegisterAddon(new VoiceChatServerAddon());
    }

    /// <inheritdoc />
    public void OnLoadGlobal(ModSettings modSettings) {
        ModSettings = modSettings ?? new ModSettings();
    }

    /// <inheritdoc />
    public ModSettings OnSaveGlobal() {
        return ModSettings;
    }
}