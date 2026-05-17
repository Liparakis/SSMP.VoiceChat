using SSMP.Api.Client;
using SsmpVoiceChat.Common;
using SsmpVoiceChat.Client.Voice;
using UnityEngine;

namespace SsmpVoiceChat.Client;

/// <summary>
/// The client-side voice chat.
/// </summary>
public class ClientVoiceChat {

    VoiceStatusIcon? VoiceStatusIcon;
    /// <summary>
    /// The logger instance for logging information.
    /// </summary>
    public static SSMP.Logging.ILogger Logger { get; private set; }

    /// <summary>
    /// The client API instance.
    /// </summary>
    private readonly IClientApi _clientApi;
    /// <summary>
    /// The network manager instance.
    /// </summary>
    private readonly ClientNetManager _netManager;
    /// <summary>
    /// The microphone manager instance.
    /// </summary>
    private readonly MicrophoneManager _micManager;
    /// <summary>
    /// The sound manager instance.
    /// </summary>
    private readonly SoundManager _soundManager;

    /// <summary>
    /// Whether the local player has their microphone muted
    /// </summary>
    private bool _muted;

    /// <summary>
    /// Whether the local player has their microphone muted and Push To Talk engaged and thus should not send any voice data.
    /// </summary>
    private bool Muted
    {
        get
        {
            if (_muted) return true;
            var key = VoiceChatMod.ModSettings.PushToTalkKey;
            var mode = VoiceChatMod.ModSettings.InputMode;
            if (key != KeyCode.None && mode != ModSettings.InputMethod.Normal)
            {
                if (mode == ModSettings.InputMethod.PushToTalk) return !Input.GetKey(key);
                return VoiceChatMod.ToggleMuted;
            }

            return false;
        }
    }

    /// <summary>
    /// Construct the client voice chat with the client addon and API.
    /// </summary>
    /// <param name="addon">The client addon instance.</param>
    /// <param name="clientApi">The client API.</param>
    /// <param name="logger">The logger instance for logging information.</param>
    public ClientVoiceChat(ClientAddon addon, IClientApi clientApi, SSMP.Logging.ILogger logger) {
        Logger = logger;

        _clientApi = clientApi;
        _netManager = new ClientNetManager(addon, clientApi.NetClient);
        _micManager = new MicrophoneManager();

        _soundManager = new SoundManager();
    }

    /// <summary>
    /// Initialize the client voice chat by registering commands and callbacks, and reloading the audio.
    /// </summary>
    public void Initialize() {
        var voiceChatCommand = new ClientVoiceChatCommand(_clientApi.UiManager.ChatBox);
        _clientApi.CommandManager.RegisterCommand(voiceChatCommand);

        VoiceChatMod.ModSettings.SetMicrophoneEvent += micName => {
            ReloadAudio();
        };
        VoiceChatMod.ModSettings.SetSpeakerEvent += speakerName => {
            ReloadAudio();
        };

        VoiceChatMod.ModSettings.SetMicFail += () =>
        {
            VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.Error);
        };

        voiceChatCommand.ToggleMuteEvent += () => {
            _muted = !_muted;

            _clientApi.UiManager.ChatBox.AddMessage($"Microphone is now {(_muted ? "" : "un")}muted");
            if (!_muted && Muted) _clientApi.UiManager.ChatBox.AddMessage($"Push To Talk is still enabled.");

            if (!Muted) VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.NotTalking);
            else if (_muted) VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.Muted);
            else VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.PushMuted);
        };

        ReloadAudio();

        _clientApi.ClientManager.ConnectEvent += OnConnect;
        _clientApi.ClientManager.DisconnectEvent += OnDisconnect;

        _clientApi.ClientManager.PlayerEnterSceneEvent += OnPlayerEnterScene;
        _clientApi.ClientManager.PlayerLeaveSceneEvent += OnPlayerLeaveScene;

        _netManager.VoiceEvent += OnVoiceReceived;
    }

    /// <summary>
    /// Callback for when the player connects to a server. This will start the microphone manager and register
    /// a listener for when voice data is received.
    /// </summary>
    private void OnConnect() {
        Logger.Debug("Client is connected, starting mic capture");

        VoiceStatusIcon = new VoiceStatusIcon();
        _micManager.Start();
        _micManager.VoiceDataEvent += OnVoiceGenerated;
        _micManager.VoiceOffEvent += OnVoiceStopped;
        ReloadAudio();
    }

    /// <summary>
    /// Callback for when the player disconnect from a server. This will deregister the listener for when voice data
    /// is received and stop the microphone manager.
    /// </summary>
    private void OnDisconnect() {
        Logger.Debug("Client is disconnected, stopping mic capture");

        VoiceStatusIcon?.DestroyIcon();
        VoiceStatusIcon = null;
        _micManager.VoiceDataEvent -= OnVoiceGenerated;
        _micManager.Stop();
        _soundManager.Close();
    }

    /// <summary>
    /// Callback method for when voice data is generated. Sends the voice data to the server if the player is not
    /// muted and connected to a server.
    /// </summary>
    /// <param name="data">The voice data as a byte array.</param>
    private void OnVoiceGenerated(byte[] data) {
        if (!_clientApi.NetClient.IsConnected) return;
        if (!Muted) {
            VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.Talking);
            _netManager.SendVoiceData(data);
        } else if (_muted) {
            VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.Muted);
        } else {
            VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.PushMuted);
        }
    }
    
    private void OnVoiceStopped() { 
        if (!Muted) VoiceStatusIcon?.SetTalking(VoiceStatusIcon.Status.NotTalking);
    }

    /// <summary>
    /// Callback method for when a player enters the local player's scene. Will create the speaker for the new player.
    /// </summary>
    /// <param name="player">The player that entered the scene.</param>
    private void OnPlayerEnterScene(IClientPlayer player) {
        Logger.Debug("Player entered scene, adding speaker");
        _soundManager.TryGetOrCreateSpeaker(player.Id, out _);
    }

    /// <summary>
    /// Callback method for when a player leaves the local player's scene. Will remove the speaker for the player.
    /// </summary>
    /// <param name="player"></param>
    private void OnPlayerLeaveScene(IClientPlayer player) {
        Logger.Debug("Player left scene, closing and removing speaker");
        _soundManager.TryRemoveSpeaker(player.Id);
    }

    /// <summary>
    /// Callback method for when voice data is received from the server.
    /// </summary>
    /// <param name="id">The ID of the player from which the voice data originates.</param>
    /// <param name="data">The voice data as a byte array.</param>
    /// <param name="proximity">Whether this voice data should be played back with proximity-based volume.</param>
    private void OnVoiceReceived(ushort id, byte[] data, bool proximity) {
        EnableRemoteStatusIcon(id);

        var decodedData = _soundManager.DecodeVoiceData(data);
        if (decodedData != null) {
            VoiceChatEvents.RaiseVoiceFrameObserved(this, new VoiceFrameEventArgs(
                VoiceFrameSource.ClientReceived,
                id,
                (byte[]) decodedData.Clone(),
                SoundManager.SampleRate,
                1,
                SoundManager.FrameLength,
                true,
                proximity
            ), message => Logger.Error(message));
        }

        if (!_soundManager.TryGetOrCreateSpeaker(id, out var speaker)) {
            Logger.Warn($"Could not get or create speaker for player '{id}', cannot play voice");
            return;
        }

        if (!proximity) {
            speaker.Play(data, decodedData);
            return;
        }

        var hc = HeroController.instance;
        if (hc == null || hc.gameObject == null) {
            Logger.Warn("Local player could not be found, cannot play voice positionally");
            speaker.Play(data, decodedData);
            return;
        }

        if (!_clientApi.ClientManager.TryGetPlayer(id, out var player)) {
            Logger.Warn($"No player found for '{id}', cannot play voice positionally");
            speaker.Play(data, decodedData);
            return;
        }

        var localPlayer = hc.gameObject;
        var localPos = localPlayer.transform.position;

        var remotePos = player.PlayerObject.transform.position;

        var pos = remotePos - localPos;

        speaker.Play(data, decodedData, new SSMP.Math.Vector3(pos.x, pos.y, pos.z));
    }

    private void EnableRemoteStatusIcon(ushort id)
    {
        if (!_clientApi.ClientManager.TryGetPlayer(id, out var player)) {
            Logger.Warn("Local player could not be found, cannot enable status indicator");
            return;
        }

        if (player == null || !player.IsInLocalScene) return;

        var container = player.PlayerContainer;
        if (container == null) {
            Logger.Warn("Local player could not be found, cannot enable status indicator");
            return;
        }

        var icon =  RemoteStatusIndicator.GetIconOnPlayerContainer(container);
        if (icon != null) icon.UpdateState(true);
    }

    /// <summary>
    /// Reload the audio, both microphone and speakers in the correct order to prevent issues with audio.
    /// </summary>
    private void ReloadAudio() {
        _micManager.Stop();
        _soundManager.Close();
        _soundManager.Open();

        Logger.Debug("Reloading Audio");

        if (_clientApi.NetClient.IsConnected) {
            _micManager.Start();
        }
    }
}