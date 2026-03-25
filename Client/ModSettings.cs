using BepInEx.Configuration;
using SsmpVoiceChat.Client.Voice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SsmpVoiceChat.Client;

/// <summary>
/// Mod settings for the voice chat mod.
/// </summary>
internal class ModSettings {
    ConfigEntry<string> _microphoneDevice;
    /// <summary>
    /// The name of the microphone device currently used.
    /// </summary>
    public string MicrophoneDeviceName => _microphoneDevice?.Value ?? "";
    /// <summary>
    /// Event that is called when the microphone is set. The parameter of the action is the device name of the
    /// microphone.
    /// </summary>
    public event Action<string> SetMicrophoneEvent = (s) => { };

    ConfigEntry<string> _speakerDevice;
    /// <summary>
    /// The name of the speaker device currently used.
    /// </summary>
    public string SpeakerDeviceName => _speakerDevice?.Value ?? "";
    /// <summary>
    /// Event that is called when the speaker is set. The parameter of the action is the device name of the speaker.
    /// </summary>
    public event Action<string> SetSpeakerEvent = (s) => { };

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


    ConfigEntry<float> _maxDistance;
    public float MaxDistance => _maxDistance.Value;

    ConfigEntry<float> _rolloffFactor;
    public float RolloffFactor => _rolloffFactor.Value;

    const string SystemDeviceName = "System Default";

    public ModSettings(ConfigFile config) {
        // Microphone
        var mics = Voice.Microphone.GetAllMicrophones();
        mics.Insert(0, SystemDeviceName);
        var micDesc = new ConfigDescription("The ID of the microphone device currently used.", new AcceptableValueList<string>(mics.ToArray()));
        _microphoneDevice = config.Bind<string>("Devices", "Microphone ID", SystemDeviceName, micDesc);
        _microphoneDevice.SettingChanged += OnMicrophoneIdChanged;
        OnMicrophoneIdChanged(null, null);

        // Speaker
        var speakers = SoundManager.GetAllDeviceSpeakers();
        speakers.Insert(0, SystemDeviceName);
        var speakerDesc = new ConfigDescription("The ID of the speaker device currently used.", new AcceptableValueList<string>(speakers.ToArray()));
        _speakerDevice = config.Bind<string>("Devices", "Speaker ID", SystemDeviceName, speakerDesc);
        _speakerDevice.SettingChanged += OnSpeakerIdChanged;
        OnSpeakerIdChanged(null, null);

        // Volume
        var ampDesc = new ConfigDescription("Modifies the volume of the microphone input.", new AcceptableValueRange<float>(0, 4));
        _microphoneAmplification = config.Bind<float>("Volume", "Microphone Amplification", 1, ampDesc);

        var volumeDesc = new ConfigDescription("The volume of the voice chat of other players.", new AcceptableValueRange<float>(0, 6));
        _voiceChatVolume = config.Bind<float>("Volume", "Chat Volume", 1, volumeDesc);
        _smoothChannelTransition = config.Bind<bool>("Volume", "Smooth Channel Transition", true, "Whether the transition between audio from a player moving from the left to the right of the local player is smooth or not");

        // Keybinds
        _pushToTalkKey = config.Bind<KeyCode>("Keybinds", "Push To Talk", KeyCode.None, "The key to press to enable your microphone. Set to None to disable push to talk");

        // Testing
        _maxDistance = config.Bind<float>("Testing", "Max Distance", 60);
        _rolloffFactor = config.Bind<float>("Testing", "Rolloff Factor", 1.5f);
    }

    string prevMicValue = "";
    private void OnMicrophoneIdChanged(object sender, EventArgs e)
    {
        if (MicrophoneDeviceName == prevMicValue) return;
        prevMicValue = MicrophoneDeviceName;

        var name = MicrophoneDeviceName;
        if (name == SystemDeviceName) name = Voice.Microphone.GetDefaultMicrophone();

        var hasMic = Voice.Microphone.GetAllMicrophones().Contains(name);
        if (!hasMic)
        {
            VoiceChatMod.ChatBox?.AddMessage($"[VC]: Couldn't find a microphone with the name {name}");
            ClientVoiceChat.Logger.Error($"[VC]: Couldn't find a microphone with the name {name}");
            return;
        }

        SetMicrophoneEvent?.Invoke(name);
    }


    string prevSpeakerValue = "";
    private void OnSpeakerIdChanged(object sender, EventArgs e)
    {
        if (SpeakerDeviceName == prevSpeakerValue) return;
        prevSpeakerValue = SpeakerDeviceName;

        var name = MicrophoneDeviceName;
        if (name == SystemDeviceName) name = SoundManager.GetDefaultDeviceSpeaker();

        var hasSpeaker = SoundManager.GetAllDeviceSpeakers().Contains(name);
        if (!hasSpeaker)
        {
            VoiceChatMod.ChatBox?.AddMessage($"[VC]: Couldn't find a speaker with the name {name}");
            ClientVoiceChat.Logger.Error($"[VC]: Couldn't find a speaker with the name {name}");
            return;
        }

        SetSpeakerEvent?.Invoke(name);
    }
}