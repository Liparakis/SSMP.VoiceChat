using System;
using System.IO;
using System.Reflection;
using SSMP.Game.Settings;
using Newtonsoft.Json;

namespace SsmpVoiceChat.Server;

/// <summary>
/// Class with server-related settings.
/// </summary>
public class ServerSettings {
    /// <summary>
    /// The file name of the JSON settings file.
    /// </summary>
    private const string FileName = "voicechat_server_settings.json";

    /// <summary>
    /// Whether the volume of voice chat should be based on the proximity of the source and listener.
    /// </summary>
    [JsonProperty("proximity_based_volume")]
    [SettingAlias("proximity", "prox")]
    public bool ProximityBasedVolume { get; set; }

    /// <summary>
    /// Whether to hear your team's voices globally independent of proximity or scenes.
    /// </summary>
    [JsonProperty("team_voices_globally")]
    [SettingAlias("teamglobal", "teamglobally")]
    public bool TeamVoicesGlobally { get; set; }

    /// <summary>
    /// Whether to hear only your team's voices and not other teams, even if they are in the same scene or in close
    /// proximity.
    /// </summary>
    [JsonProperty("team_voices_only")]
    [SettingAlias("teamonly")]
    public bool TeamVoicesOnly { get; set; }

    /// <summary>
    /// Save the server settings to file.
    /// </summary>
    public void SaveToFile() {
        var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dirName == null) {
            return;
        }

        var filePath = Path.Combine(dirName, FileName);
        var settingsJson = JsonConvert.SerializeObject(this, Formatting.Indented);

        try {
            File.WriteAllText(filePath, settingsJson);
        } catch (Exception e) {
            ServerVoiceChat.Logger.Error($"Could not write server settings to file:\n{e}");
        }
    }

    /// <summary>
    /// Load the server settings from file.
    /// </summary>
    /// <returns>An instance with the loaded settings or a new instance if it could not be loaded.</returns>
    public static ServerSettings LoadFromFile() {
        var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dirName == null) {
            return new ServerSettings();
        }

        var filePath = Path.Combine(dirName, FileName);
        if (!File.Exists(filePath)) {
            var settings = new ServerSettings();
            settings.SaveToFile();
            return settings;
        }

        try {
            var fileContents = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<ServerSettings>(fileContents);
            return settings ?? new ServerSettings();
        } catch (Exception e) {
            ServerVoiceChat.Logger.Error($"Could not load server settings from file:\n{e}");
            return new ServerSettings();
        }
    }
}