using System;

namespace SsmpVoiceChat.Client.Voice; 

/// <summary>
/// Static class for managing volume levels of audio data.
/// </summary>
public static class VolumeManager {
    /// <summary>
    /// The maximum amplification that audio can have.
    /// </summary>
    private const short MaxAmplification = short.MaxValue - 1;

    /// <summary>
    /// Array containing the maximum amplification possible for a specific array of audio data. Used to keep a steady
    /// amplification based on previous samples to prevent sudden jumps in volume.
    /// </summary>
    private static readonly float[] MaxMultipliers;
    /// <summary>
    /// The current index for storing the new maximum amplification in the array.
    /// </summary>
    private static int _index;

    static VolumeManager() {
        MaxMultipliers = new float[50];
    }

    /// <summary>
    /// Amplifies the given audio data. Will check a number of previous samples to pick a proper amplification to
    /// smooth out amplification and prevent sudden jumps in volume.
    /// </summary>
    /// <param name="audio">The audio data in 16-bit mono.</param>
    /// <param name="multiplier">The requested amplification as a multiplier.</param>
    /// <returns>The new amplified audio data.</returns>
    public static short[] AmplifyAudioData(short[] audio, float multiplier) {
        // Get the maximum amplification possible for the audio and store it in the array
        MaxMultipliers[_index] = GetMaximumAmplification(audio, multiplier);
        _index = (_index + 1) % MaxMultipliers.Length;

        // Find the minimum multiplier for the last samples that were stored
        // Initialized at 1 so as a fallback we don't do any amplification
        var min = -1f;
        foreach (var mul in MaxMultipliers) {
            if (mul < 0f) {
                continue;
            }

            if (min < 0f) {
                min = mul;
                continue;
            }
            
            if (mul < min) {
                min = mul;
            }
        }

        if (min < 0f) {
            min = 1f;
        }

        var maxMultiplier = Math.Min(min, multiplier);

        // Multiple each sample in the audio with the new multiplier
        var newAudio = new short[audio.Length];
        for (var i = 0; i < audio.Length; i++) {
            newAudio[i] = (short) (audio[i] * maxMultiplier);
        }

        return newAudio;
    }

    /// <summary>
    /// Get the maximum possible amplification for the given audio data and the requested amplification.
    /// </summary>
    /// <param name="audio">The audio data in 16-bit mono.</param>
    /// <param name="multiplier">The requested amplification as a multiplier.</param>
    /// <returns>The maximum possible amplification that will make sure that the range of the audio data is within
    /// the short range.</returns>
    private static float GetMaximumAmplification(short[] audio, float multiplier) {
        short max = 0;

        foreach (var value in audio) {
            short abs;
            
            // If the value is the minimum value of a short and we would take the absolute, it would exceed the
            // maximum of a short, so we take the maximum short value in that case directly
            if (value == short.MinValue) {
                abs = short.MaxValue;
            } else {
                abs = Math.Abs(value);
            }

            if (abs > max) {
                max = abs;
            }
        }

        if (max == 0) {
            return 1f;
        }

        return Math.Min(multiplier, MaxAmplification / (float) max);
    }
}