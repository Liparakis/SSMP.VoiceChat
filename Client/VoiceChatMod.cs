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
    internal static ModSettings ModSettings;
    internal static IChatBox ChatBox;

    /// <inheritdoc />
    public void Awake() {
        ClientAddon.RegisterAddon(new VoiceChatClientAddon());
        ServerAddon.RegisterAddon(new VoiceChatServerAddon());
        ModSettings = new ModSettings(Config);
    }
}