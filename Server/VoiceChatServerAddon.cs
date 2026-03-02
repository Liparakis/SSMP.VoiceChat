using SSMP.Api.Server;

namespace SsmpVoiceChat.Server;

/// <summary>
/// The server-side voice chat addon class.
/// </summary>
public class VoiceChatServerAddon : ServerAddon {
    /// <inheritdoc />
    public override void Initialize(IServerApi serverApi) {
        new ServerVoiceChat(this, serverApi, Logger).Initialize();
    }

    /// <inheritdoc />
    protected override string Name => Identifier.AddonName;
    /// <inheritdoc />
    protected override string Version => Identifier.AddonVersion;
    /// <inheritdoc />
    public override uint ApiVersion => Identifier.ApiVersion;
    /// <inheritdoc />
    public override bool NeedsNetwork => true;
}