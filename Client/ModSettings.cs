using BepInEx.Configuration;
using SsmpVoiceChat.Client.Voice;
using SsmpVoiceChat.Common;
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
    public string MicrophoneDeviceName { get
        {
            var value = _microphoneDevice?.Value ?? "";
            if (value == SystemDeviceName) return Voice.Microphone.GetDefaultMicrophone();

            return value;
        }
    }
    /// <summary>
    /// Event that is called when the microphone is set. The parameter of the action is the device name of the
    /// microphone.
    /// </summary>
    public event Action<string> SetMicrophoneEvent = (s) => { };

    ConfigEntry<string> _speakerDevice;
    /// <summary>
    /// The name of the speaker device currently used.
    /// </summary>
    public string SpeakerDeviceName { get
        {
            var value = _speakerDevice?.Value ?? "";
            if (value == SystemDeviceName) return SoundManager.GetDefaultDeviceSpeaker();

            return value;
        }
    }
    /// <summary>
    /// Event that is called when the speaker is set. The parameter of the action is the device name of the speaker.
    /// </summary>
    public event Action<string> SetSpeakerEvent = (s) => { };

    ConfigEntry<int> _microphoneAmplification;
    /// <summary>
    /// The microphone amplification for modifying the volume of the microphone input.
    /// </summary>
    public float MicrophoneAmplification => Mathf.Clamp((float)_microphoneAmplification.Value / 5, 0, 3);

    ConfigEntry<int> _voiceChatVolume;
    /// <summary>
    /// The volume of the voice chat of other players.
    /// </summary>
    public float VoiceChatVolume => Mathf.Clamp((float)_voiceChatVolume.Value / 10, 0, 1);

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
    public float MaxDistance => _maxDistance?.Value ?? 60;

    ConfigEntry<float> _rolloffFactor;
    public float RolloffFactor => _rolloffFactor?.Value ?? 1.5f;

    public const string SystemDeviceName = "System Default";

    public ModSettings(ConfigFile config) {
        // Microphone
        var mics = Voice.Microphone.GetAllMicrophones();
        //mics = mics.Prepend(SystemDeviceName).ToList();
        mics.Insert(0, SystemDeviceName);
        var micDesc = new ConfigDescription("The microphone device currently used.", new AcceptableValueList<string>(mics.ToArray()));
        _microphoneDevice = config.Bind<string>("Devices", "Microphone", SystemDeviceName, micDesc);
        _microphoneDevice.SettingChanged += OnMicrophoneChanged;
        OnMicrophoneChanged(null, null);

        // Speaker
        var speakers = SoundManager.GetAllDeviceSpeakers();
        //speakers = speakers.Prepend(SystemDeviceName).ToList();
        speakers.Insert(0, SystemDeviceName);
        var speakerDesc = new ConfigDescription("The speaker device currently used.", new AcceptableValueList<string>(speakers.ToArray()));
        _speakerDevice = config.Bind<string>("Devices", "Speaker", SystemDeviceName, speakerDesc);
        _speakerDevice.SettingChanged += OnSpeakerChanged;
        OnSpeakerChanged(null, null);

        // Volume
        var ampDesc = new ConfigDescription("Modifies the volume of the microphone input.", new AcceptableValueRange<int>(0, 15));
        _microphoneAmplification = config.Bind<int>("Volume", "Microphone Amplification", 5, ampDesc);

        var volumeDesc = new ConfigDescription("The volume of the voice chat of other players.", new AcceptableValueRange<int>(0, 10));
        _voiceChatVolume = config.Bind<int>("Volume", "Chat Volume", 6, volumeDesc);
        _smoothChannelTransition = config.Bind<bool>("Volume", "Smooth Channel Transition", true, "Whether the transition between audio from a player moving from the left to the right of the local player is smooth or not");

        // Keybinds
        _pushToTalkKey = config.Bind<KeyCode>("Keybinds", "Push To Talk", KeyCode.None, "The key to press to enable your microphone. Set to None to disable push to talk");

        // Testing
        _maxDistance = config.Bind<float>("Testing", "Max Distance", 60);
        _rolloffFactor = config.Bind<float>("Testing", "Rolloff Factor", 1.5f);
    }

    string prevMicValue = "";
    private void OnMicrophoneChanged(object sender, EventArgs e)
    {
        if (MicrophoneDeviceName == prevMicValue) return;
        prevMicValue = MicrophoneDeviceName;

        var name = MicrophoneDeviceName;

        var hasMic = Voice.Microphone.GetAllMicrophones().Contains(name);
        if (!hasMic)
        {
            VoiceChatMod.ChatBox?.AddMessage($"[VC]: Couldn't find a microphone with the name {name}");
            ClientVoiceChat.Logger?.Error($"[VC]: Couldn't find a microphone with the name {name}");
            //Debug.LogError($"[VC]: Couldn't find a microphone with the name {name}");
            return;
        }

        SetMicrophoneEvent?.Invoke(name);
    }


    string prevSpeakerValue = "";
    private void OnSpeakerChanged(object sender, EventArgs e)
    {
        if (SpeakerDeviceName == prevSpeakerValue) return;
        prevSpeakerValue = SpeakerDeviceName;

        var name = SpeakerDeviceName;

        var hasSpeaker = SoundManager.GetAllDeviceSpeakers().Contains(name);
        if (!hasSpeaker)
        {
            VoiceChatMod.ChatBox?.AddMessage($"[VC]: Couldn't find a speaker with the name {name}");
            ClientVoiceChat.Logger?.Error($"[VC]: Couldn't find a speaker with the name {name}");
            //Debug.LogError($"[VC]: Couldn't find a speaker with the name {name}");
            return;
        }

        SetSpeakerEvent?.Invoke(name);
    }
}