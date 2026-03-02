using System;
using System.Collections.Generic;
using System.Linq;
using SSMP.Api.Client;
using SSMP.Api.Command.Client;
using SsmpVoiceChat.Client.Voice;
using SsmpVoiceChat.Common.Command;

namespace SsmpVoiceChat.Client;

/// <summary>
/// Command for the client-side voice chat.
/// </summary>
public class ClientVoiceChatCommand : IClientCommand {
    /// <inheritdoc />
    public string Trigger => "/voicechatclient";

    /// <inheritdoc />
    public string[] Aliases => ["/vcc"];

    /// <summary>
    /// Event that is called when the microphone is set. The parameter of the action is the device name of the
    /// microphone.
    /// </summary>
    public event Action<string> SetMicrophoneEvent;
    /// <summary>
    /// Event that is called when the speaker is set. The parameter of the action is the device name of the speaker.
    /// </summary>
    public event Action<string> SetSpeakerEvent;
    /// <summary>
    /// Event that is called when the user toggles mute through this command.
    /// </summary>
    public event Action ToggleMuteEvent;

    /// <summary>
    /// The chat box to post messages to from this command.
    /// </summary>
    private readonly IChatBox _chatBox;

    /// <summary>
    /// Dictionary for mapping indices to microphone device names for setting the microphone.
    /// </summary>
    private readonly Dictionary<int, string> _microphoneNames;
    /// <summary>
    /// Dictionary for mapping indices to speaker device names for setting the speaker.
    /// </summary>
    private readonly Dictionary<int, string> _speakerNames;

    public ClientVoiceChatCommand(IChatBox chatBox) {
        _chatBox = chatBox;
        _microphoneNames = new Dictionary<int, string>();
        _speakerNames = new Dictionary<int, string>();
    }

    /// <inheritdoc />
    public void Execute(string[] args) {
        void SendUsage() {
            _chatBox.AddMessage($"Invalid usage: {Trigger} <mute|volume|device|set>");
        }

        if (args.Length < 2) {
            SendUsage();
            return;
        }

        var action = args[1];
        if (action == "mute") {
            HandleMute();
        } else if (action == "volume") {
            HandleVolume(args);
        } else if (action == "device") {
            HandleDevice(args);
        } else if (action == "set") {
            HandleSet(args);
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Handle the mute sub-command.
    /// </summary>
    private void HandleMute() {
        ToggleMuteEvent?.Invoke();
    }

    /// <summary>
    /// Handle the volume sub-command.
    /// </summary>
    private void HandleVolume(string[] args) {
        void SendUsage() {
            _chatBox.AddMessage($"Invalid usage: {Trigger} volume <mic|speaker> <value>");
        }

        if (args.Length < 4) {
            SendUsage();
            return;
        }

        var action = args[2];
        var value = args[3];
        if (action == "mic") {
            void SendMicUsage() {
                SendUsage();
                _chatBox.AddMessage($"Invalid microphone amplification value '{value}', please provide a value between 0 and 4");
            }
            
            if (!float.TryParse(value, out var floatValue)) {
                SendMicUsage();
                return;
            }

            if (floatValue <= 0 || floatValue > 4) {
                SendMicUsage();
                return;
            }

            VoiceChatMod.ModSettings.MicrophoneAmplification = floatValue;
            _chatBox.AddMessage($"Set microphone amplification value to '{value}'");
        } else if (action == "speaker") {
            void SendSpeakerUsage() {
                SendUsage();
                _chatBox.AddMessage($"Invalid speaker volume '{value}', please provide a value between 0 and 6");
            }
            
            if (!float.TryParse(value, out var floatValue)) {
                SendSpeakerUsage();
                return;
            }

            if (floatValue < 0 || floatValue > 6) {
                SendSpeakerUsage();
                return;
            }

            VoiceChatMod.ModSettings.VoiceChatVolume = floatValue;
            _chatBox.AddMessage($"Set speaker volume to '{value}'");
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Handle the device sub-command.
    /// </summary>
    private void HandleDevice(string[] args) {
        void SendUsage() {
            _chatBox.AddMessage($"Invalid usage: {Trigger} device <list|set>");
        }

        if (args.Length < 3) {
            SendUsage();
            return;
        }

        var action = args[2];
        if (action == "list") {
            HandleDeviceList(args);
        } else if (action == "set") {
            HandleDeviceSet(args);
        }
    }

    /// <summary>
    /// Handle the device list sub-command.
    /// </summary>
    private void HandleDeviceList(string[] args) {
        void SendUsage() {
            _chatBox.AddMessage($"Invalid usage: {Trigger} device list <mics|speakers>");
        }

        if (args.Length < 4) {
            SendUsage();
            return;
        }

        var type = args[3];
        if (type is "mics" or "mic") {
            var mics = Microphone.GetAllMicrophones();
            if (mics.Count == 0) {
                _chatBox.AddMessage("No microphones could be found");
                return;
            }

            _microphoneNames.Clear();

            _chatBox.AddMessage("Microphones (id, name):");

            var index = 1;

            foreach (var mic in mics) {
                _chatBox.AddMessage($"{index}: {mic}");

                _microphoneNames[index++] = mic;
            }
        } else if (type is "speakers" or "speaker") {
            var speakers = SoundManager.GetAllDeviceSpeakers();
            if (speakers.Count == 0) {
                _chatBox.AddMessage("No speakers could be found");
                return;
            }

            _speakerNames.Clear();

            _chatBox.AddMessage("Speakers (id, name):");

            var index = 1;

            foreach (var speaker in speakers) {
                _chatBox.AddMessage($"{index}: {speaker}");

                _speakerNames[index++] = speaker;
            }
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Handle the device set sub-command.
    /// </summary>
    private void HandleDeviceSet(string[] args) {
        void SendUsage() {
            _chatBox.AddMessage($"Invalid usage: {Trigger} device set <mic|speaker> <value>");
        }

        if (args.Length < 5) {
            SendUsage();
            return;
        }

        var type = args[3];
        var value = args[4];
        if (type is "mic" or "speaker") {
            var isInt = int.TryParse(value, out var intValue);

            if (type is "mic") {
                if (isInt) {
                    if (_microphoneNames.TryGetValue(intValue, out var micName)) {
                        SetMicrophoneEvent?.Invoke(micName);

                        _chatBox.AddMessage($"Set microphone to \"{micName}\"");

                        return;
                    }
                }

                var micNames = _microphoneNames.Values;
                if (micNames.Contains(value)) {
                    SetMicrophoneEvent?.Invoke(value);

                    _chatBox.AddMessage($"Set microphone to \"{value}\"");
                    return;
                }

                _chatBox.AddMessage($"Could not find microphone with ID or name: \"{value}\"");
            } else if (type is "speaker") {
                if (isInt) {
                    if (_speakerNames.TryGetValue(intValue, out var speakerName)) {
                        SetSpeakerEvent?.Invoke(speakerName);

                        _chatBox.AddMessage($"Set speaker to \"{speakerName}\"");

                        return;
                    }
                }

                var speakerNames = _speakerNames.Values;
                if (speakerNames.Contains(value)) {
                    SetSpeakerEvent?.Invoke(value);

                    _chatBox.AddMessage($"Set speaker to \"{value}\"");
                    return;
                }

                _chatBox.AddMessage($"Could not find speaker with ID or name: \"{value}\"");
            }
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Handle the set sub-command.
    /// </summary>
    private void HandleSet(string[] args) {
        CommandUtil.HandleSetCommand(
            Trigger,
            args,
            VoiceChatMod.ModSettings,
            _chatBox.AddMessage,
            () => VoiceChatMod.ModSettings.SaveToFile(),
            true
        );
    }
}