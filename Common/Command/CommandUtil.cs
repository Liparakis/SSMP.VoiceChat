using System;
using System.Linq;
using System.Reflection;
using SSMP.Game.Settings;

namespace SsmpVoiceChat.Common.Command;

/// <summary>
/// Static class for command utility methods.
/// </summary>
public static class CommandUtil {
    /// <summary>
    /// Handle the set subcommand of both client and server commands.
    /// </summary>
    /// <param name="trigger">The trigger of the command.</param>
    /// <param name="args">The arguments of the command.</param>
    /// <param name="settings">The setting class instance for reading and setting values for.</param>
    /// <param name="feedbackAction">The action to execute when providing feedback about the result of the command.
    /// </param>
    /// <param name="successAction">The action to execute when the command completes successfully.</param>
    /// <param name="requireSettingAliasAttribute">Whether only variables/properties from the setting class are
    /// included if they have a setting alias attribute on them.</param>
    /// <typeparam name="TSettings">The type of the settings class.</typeparam>
    public static void HandleSetCommand<TSettings>(
        string trigger,
        string[] args,
        TSettings settings,
        Action<string> feedbackAction,
        Action successAction = null,
        bool requireSettingAliasAttribute = false
    ) {
        var propertyInfos = typeof(TSettings).GetProperties();

        if (args.Length < 3) {
            feedbackAction?.Invoke($"Available settings: {string.Join(", ", propertyInfos.Select(p => p.Name))}");
            return;
        }

        var settingName = args[2];


        PropertyInfo settingProperty = null;
        foreach (var prop in propertyInfos) {
            var aliasAttribute = prop.GetCustomAttribute<SettingAliasAttribute>();
            if (aliasAttribute == null && requireSettingAliasAttribute) {
                continue;
            }
            
            settingName = settingName.ToLower().Replace("_", "");
            
            // Check if the property equals the setting name given as argument ignoring capitalization
            if (prop.Name.ToLower().Equals(settingName)) {
                settingProperty = prop;
                break;
            }
            
            // Alternatively check for alias attribute and all aliases
            if (aliasAttribute != null) {
                if (aliasAttribute.Aliases.Contains(settingName)) {
                    settingProperty = prop;
                    break;
                }
            }
        }

        if (settingProperty == null || !settingProperty.CanRead) {
            feedbackAction?.Invoke($"Could not find setting with name: {settingName}");
            return;
        }

        if (args.Length < 4) {
            // User did not provide value to write setting, so we print the value
            var currentValue = settingProperty.GetValue(settings);

            feedbackAction?.Invoke($"Setting '{settingName}' currently has value: {currentValue}");
            return;
        }

        if (!settingProperty.CanWrite) {
            feedbackAction?.Invoke($"Could not change value of setting with name: {settingName} (non-writable)");
            return;
        }

        var newValueString = args[3];
        object newValueObject;

        if (settingProperty.PropertyType == typeof(int)) {
            if (!int.TryParse(newValueString, out var newValueInt)) {
                feedbackAction?.Invoke("Please provide an integer value for this setting");
                return;
            }

            newValueObject = newValueInt;
        } else if (settingProperty.PropertyType == typeof(bool)) {
            if (!bool.TryParse(newValueString, out var newValueBool)) {
                feedbackAction?.Invoke("Please provide a boolean value for this setting");
                return;
            }

            newValueObject = newValueBool;
        } else if (settingProperty.PropertyType == typeof(float)) {
            if (!float.TryParse(newValueString, out var newValueFloat)) {
                feedbackAction?.Invoke("Please provide a float value for this setting");
                return;
            }

            newValueObject = newValueFloat;
        } else {
            feedbackAction?.Invoke(
                $"Could not change value of setting with name: {settingName} (unhandled type)");
            return;
        }

        settingProperty.SetValue(settings, newValueObject);

        feedbackAction?.Invoke($"Changed setting '{settingName}' to: {newValueObject}");

        successAction?.Invoke();
    }
}