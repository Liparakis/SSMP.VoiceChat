using Newtonsoft.Json;
using SSMP.Game.Settings;
using SsmpVoiceChat.Server;
using System.IO;
using System.Reflection;
using System;

namespace SsmpVoiceChat.Client; 

/// <summary>
/// Mod settings for the voice chat mod.
/// </summary>
public class ModSettingsBkp {
    /// <summary>
    /// The file name of the JSON settings file.
    /// </summary>
    private const string FileName = "voicechat_client_settings.json";
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

    /// <summary>
    /// Save the client settings to file.
    /// </summary>
    public void SaveToFile()
    {
        var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dirName == null)
        {
            return;
        }

        var filePath = Path.Combine(dirName, FileName);
        var settingsJson = JsonConvert.SerializeObject(this, Formatting.Indented);

        try
        {
            File.WriteAllText(filePath, settingsJson);
        }
        catch (Exception e)
        {
            ServerVoiceChat.Logger.Error($"Could not write server settings to file:\n{e}");
        }
    }

    /// <summary>
    /// Load the client settings from file.
    /// </summary>
    /// <returns>An instance with the loaded settings or a new instance if it could not be loaded.</returns>
    public static ModSettingsBkp LoadFromFile()
    {
        var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dirName == null)
        {
            return new ModSettingsBkp();
        }

        var filePath = Path.Combine(dirName, FileName);
        if (!File.Exists(filePath))
        {
            var settings = new ModSettingsBkp();
            settings.SaveToFile();
            return settings;
        }

        try
        {
            var fileContents = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<ModSettingsBkp>(fileContents);
            return settings ?? new ModSettingsBkp();
        }
        catch (Exception e)
        {
            ServerVoiceChat.Logger.Error($"Could not load server settings from file:\n{e}");
            return new ModSettingsBkp();
        }
    }
}