using BepInEx.Configuration;
using SsmpVoiceChat.Client.Voice;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SsmpVoiceChat.Client;

/// <summary>
/// Mod settings for the voice chat mod.
/// </summary>
internal class ModSettings {
    ConfigEntry<int> _microphoneDeviceId;
    /// <summary>
    /// The name of the microphone device currently used.
    /// </summary>
    public string MicrophoneDeviceName;
    /// <summary>
    /// Event that is called when the microphone is set. The parameter of the action is the device name of the
    /// microphone.
    /// </summary>
    public event Action<string> SetMicrophoneEvent;

    ConfigEntry<int> _speakerDeviceId;
    /// <summary>
    /// The name of the speaker device currently used.
    /// </summary>
    public string SpeakerDeviceName;
    /// <summary>
    /// Event that is called when the speaker is set. The parameter of the action is the device name of the speaker.
    /// </summary>
    public event Action<string> SetSpeakerEvent;

    ConfigEntry<float> _microphoneAmplification;
    /// <summary>
    /// The microphone amplification for modifying the volume of the microphone input.
    /// </summary>
    public float MicrophoneAmplification => Mathf.Clamp(_microphoneAmplification.Value, 0, 4);

    ConfigEntry<float> _voiceChatVolume;
    /// <summary>
    /// The volume of the voice chat of other players.
    /// </summary>
    public float VoiceChatVolume => Mathf.Clamp(_voiceChatVolume.Value, 0, 4);

    ConfigEntry<bool> _smoothChannelTransition;
    /// <summary>
    /// Whether the transition between audio from a player moving from the left to the right of the local player is
    /// smooth or not.
    /// </summary>
    public bool SmoothChannelTransition => _smoothChannelTransition.Value;


    ConfigEntry<KeyCode> _pushToTalkKey;
    /// <summary>
    /// The key to press to enable your microphone. Set to None to disable push to talk
    /// </summary>
    public KeyCode PushToTalkKey => _pushToTalkKey.Value;

    public ModSettings(ConfigFile config) {
        var defaultMicStr = Voice.Microphone.GetDefaultMicrophone();
        var defaultMicIndex = Voice.Microphone.GetAllMicrophones().IndexOf(defaultMicStr);

        _microphoneDeviceId = config.Bind<int>("Devices", "Microphone ID", defaultMicIndex + 1, "The ID of the microphone device currently used.");
        _microphoneDeviceId.SettingChanged += OnMicrophoneIdChanged;
        OnMicrophoneIdChanged(null, null);

        var defaultSpeakerStr = SoundManager.GetDefaultDeviceSpeaker();
        var defaultSpeakerIndex = SoundManager.GetAllDeviceSpeakers().IndexOf(defaultSpeakerStr);

        _speakerDeviceId = config.Bind<int>("Devices", "Speaker ID", defaultSpeakerIndex + 1, "The ID of the speaker device currently used.");
        _speakerDeviceId.SettingChanged += OnSpeakerIdChanged;
        OnSpeakerIdChanged(null, null);

        _microphoneAmplification = config.Bind<float>("Volume", "Microphone Amplification", 1, "Modifies the volume of the microphone input.");
        
        _voiceChatVolume = config.Bind<float>("Volume", "Chat Volume", 1, "The volume of the voice chat of other players.");
        _smoothChannelTransition = config.Bind<bool>("Volume", "Smooth Channel Transition", true, "Whether the transition between audio from a player moving from the left to the right of the local player is smooth or not");

        _pushToTalkKey = config.Bind<KeyCode>("Keybinds", "Push To Talk", KeyCode.None, "The key to press to enable your microphone. Set to None to disable push to talk");
    }

    private void OnMicrophoneIdChanged(object sender, EventArgs e)
    {
        var id = _microphoneDeviceId.Value - 1;
        var mics = Voice.Microphone.GetAllMicrophones();
        if (id < 0 || id > mics.Count - 1)
        {
            VoiceChatMod.ChatBox.AddMessage($"[VC]: Couldn't find a microphone with ID {_microphoneDeviceId}");
            return;
        }

        var mic = mics[id];
        MicrophoneDeviceName = mic;
        SetMicrophoneEvent?.Invoke(mic);
    }

    private void OnSpeakerIdChanged(object sender, EventArgs e)
    {
        var id = _speakerDeviceId.Value - 1;
        var speakers = SoundManager.GetAllDeviceSpeakers();
        if (id < 0 || id > speakers.Count - 1)
        {
            VoiceChatMod.ChatBox.AddMessage($"[VC]: Couldn't find a speaker with ID {_speakerDeviceId}");
            return;
        }

        var speaker = speakers[id];
        SpeakerDeviceName = speaker;
        SetSpeakerEvent?.Invoke(speaker);
    }
}
