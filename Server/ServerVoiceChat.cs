using System;
using System.Collections.Generic;
using SSMP.Api.Server;
using SSMP.Game;
using SSMP.Logging;
using SsmpVoiceChat.Common;
using SsmpVoiceChat.Common.Opus;

namespace SsmpVoiceChat.Server; 

/// <summary>
/// The server-side voice chat.
/// </summary>
public class ServerVoiceChat {
    /// <summary>
    /// The logger instance for logging information.
    /// </summary>
    public static ILogger Logger { get; private set; }

    /// <summary>
    /// The server API instance.
    /// </summary>
    private readonly IServerApi _serverApi;
    /// <summary>
    /// The network manager instance.
    /// </summary>
    private readonly ServerNetManager _netManager;
    /// <summary>
    /// The server settings instance.
    /// </summary>
    private readonly ServerSettings _settings;

    /// <summary>
    /// Set of player IDs for players that are currently broadcasting their voice.
    /// </summary>
    private readonly HashSet<ushort> _broadcasters;

    /// <summary>
    /// Decoder used to expose server-received PCM frames to downstream mods.
    /// </summary>
    private readonly OpusCodec _decoder;

    /// <summary>
    /// Construct the server voice chat with the server addon and API.
    /// </summary>
    /// <param name="addon">The server addon instance.</param>
    /// <param name="serverApi">The server API.</param>
    /// <param name="logger">The logger instance for logging information.</param>
    public ServerVoiceChat(ServerAddon addon, IServerApi serverApi, ILogger logger) {
        Logger = logger;

        _serverApi = serverApi;
        _netManager = new ServerNetManager(addon, serverApi.NetServer);

        _settings = ServerSettings.LoadFromFile();

        _broadcasters = [];
        _decoder = new OpusCodec();
    }

    /// <summary>
    /// Initialize the server voice chat by registering commands and callbacks.
    /// </summary>
    public void Initialize() {
        _serverApi.CommandManager.RegisterCommand(new ServerVoiceChatCommand(_settings, _broadcasters));

        _netManager.VoiceEvent += OnVoice;
    }

    /// <summary>
    /// Callback for when voice data is received from a player.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="data">The voice data.</param>
    private void OnVoice(ushort id, byte[] data) {
        try {
            var decodedData = _decoder.Decode(data);
            VoiceChatEvents.RaiseVoiceFrameObserved(this, new VoiceFrameEventArgs(
                VoiceFrameSource.ServerReceived,
                id,
                (byte[]) decodedData.Clone(),
                48000,
                1,
                20,
                true,
                false
            ), message => Logger.Error(message));
        } catch (Exception exception) {
            Logger.Warn($"Failed to decode received server voice frame for observation:\n{exception}");
        }

        if (!_serverApi.ServerManager.TryGetPlayer(id, out var sender)) {
            Logger.Warn($"Could not find player '{id}' for received voice data");
            return;
        }

        var senderTeam = sender.Team;

        foreach (var receiver in _serverApi.ServerManager.Players) {
            if (sender == receiver) {
                continue;
            }

            if (_broadcasters.Contains(sender.Id)) {
                _netManager.SendVoiceData(receiver.Id, sender.Id, data, false);
                continue;
            }

            var sameTeam = senderTeam == receiver.Team && senderTeam != Team.None;

            if (_settings.TeamVoicesOnly && !sameTeam) {
                continue;
            }

            if (_settings.TeamVoicesGlobally && sameTeam) {
                _netManager.SendVoiceData(receiver.Id, sender.Id, data, false);
                continue;
            }

            if (sender.CurrentScene != receiver.CurrentScene) {
                continue;
            }

            _netManager.SendVoiceData(receiver.Id, sender.Id, data, _settings.ProximityBasedVolume);
        }
    }
}
