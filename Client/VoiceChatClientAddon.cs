using SSMP.Api.Client;

namespace SsmpVoiceChat.Client;

/// <summary>
/// The client-side voice chat addon class.
/// </summary>
public class VoiceChatClientAddon : ClientAddon {
    /// <inheritdoc />
    public override void Initialize(IClientApi clientApi) {
        new ClientVoiceChat(this, clientApi, Logger).Initialize();
    }

    /// <inheritdoc />
    protected override string Name => Identifier.AddonName;
    /// <inheritdoc />
    protected override string Version => Identifier.AddonVersion;
    /// <inheritdoc />
    public override bool NeedsNetwork => true;
}