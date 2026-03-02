using System;

namespace SsmpVoiceChat.Common;

/// <summary>
/// Static class for utility methods for converting audio data to different formats.
/// </summary>
public static class DataUtils {
    /// <summary>
    /// The scale for converting floats to shorts.
    /// </summary>
    private const float FloatShortScale = short.MaxValue;
    /// <summary>
    /// Float value to use as a clip-off point for floats.
    /// </summary>
    private const float FloatClip = FloatShortScale - 1;
    /// <summary>
    /// Float short scaling factor, or the inverse of <see cref="FloatShortScale"/>.
    /// </summary>
    private const float FloatShortScalingFactor = 1f / FloatShortScale;

    /// <summary>
    /// Convert the given float array to an array of shorts by normalizing the floats in the range of shorts.
    /// </summary>
    /// <param name="audioData">An array of floats representing audio data.</param>
    /// <returns>An array of shorts as converted audio data.</returns>
    public static short[] FloatsToShortsNormalized(float[] audioData) {
        var shortAudioData = new short[audioData.Length];
        for (var i = 0; i < audioData.Length; i++) {
            shortAudioData[i] = (short) Math.Max(Math.Min(audioData[i] * FloatShortScale, FloatClip), -FloatShortScale);
        }

        return shortAudioData;
    }

    /// <summary>
    /// Convert the given array of shorts to an array of bytes of twice the length. Each short will take up two bytes
    /// in the resulting array.
    /// </summary>
    /// <param name="shorts">An array of shorts.</param>
    /// <returns>An array of bytes where each two bytes represent their respective short in the input.</returns>
    public static byte[] ShortsToBytes(short[] shorts) {
        var bytes = new byte[shorts.Length * 2];
        for (var i = 0; i < shorts.Length; i++) {
            var s = shorts[i];
            bytes[i * 2] = (byte) (s & 0xFF);
            bytes[i * 2 + 1] = (byte) ((s >> 8) & 0xFF);
        }

        return bytes;
    }

    /// <summary>
    /// Convert the given array of bytes to an array of shorts of half the length. Each two bytes will take up one
    /// short in the resulting array. 
    /// </summary>
    /// <param name="bytes">An array of bytes.</param>
    /// <returns>An array of shorts where each short represents their respective two bytes in the input.</returns>
    /// <exception cref="ArgumentException">Thrown if the length of the given byte array is not divisible by two.
    /// </exception>
    public static short[] BytesToShorts(byte[] bytes) {
        if (bytes.Length % 2 != 0) {
            throw new ArgumentException("Byte array length must be even", nameof(bytes));
        }

        var shorts = new short[bytes.Length / 2];
        for (var i = 0; i < bytes.Length; i += 2) {
            var b1 = bytes[i];
            var b2 = bytes[i + 1];
            shorts[i / 2] = (short) (((b2 & 0xFF) << 8) | (b1 & 0xFF));
        }

        return shorts;
    }
}