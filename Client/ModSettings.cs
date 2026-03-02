using SSMP.Game.Settings;
using Newtonsoft.Json;

namespace SsmpVoiceChat.Client; 

/// <summary>
/// Mod settings for the voice chat mod.
/// </summary>
public class ModSettings {
    /// <summary>
    /// The name of the microphone device currently used.
    /// </summary>
    [JsonProperty("microphone_device_name")]
    public string MicrophoneDeviceName { get; set; }

    /// <summary>
    /// The name of the speaker device currently used.
    /// </summary>
    [JsonProperty("speaker_device_name")]
    public string SpeakerDeviceName { get; set; }

    /// <summary>
    /// The microphone amplification for modifying the volume of the microphone input.
    /// </summary>
    [JsonProperty("microphone_amplification")]
    [SettingAlias("micvol", "micvolume", "micamp")]
    public float MicrophoneAmplification { get; set; } = 1f;

    /// <summary>
    /// The volume of the voice chat of other players.
    /// </summary>
    [JsonProperty("voice_chat_volume")]
    [SettingAlias("speakervol", "speakervolume")]
    public float VoiceChatVolume { get; set; } = 1f;

    /// <summary>
    /// Whether the transition between audio from a player moving from the left to the right of the local player is
    /// smooth or not.
    /// </summary>
    [JsonProperty("smooth_channel_transition")]
    [SettingAlias("smoothaudio")]
    public bool SmoothChannelTransition { get; set; } = true;
}