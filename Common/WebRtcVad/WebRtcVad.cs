using System;

namespace SsmpVoiceChat.Common.WebRtcVad; 

/// <summary>
/// Class for the Web RTC VAD library for detecting voice in microphone input.
/// </summary>
public class WebRtcVad : IDisposable {
    /// <summary>
    /// Int pointer to the handle for the library.
    /// </summary>
    private IntPtr _handle;

    /// <summary>
    /// Sample rate in Hertz of data.
    /// </summary>
    private int _sampleRate;
    /// <summary>
    /// Frame size in samples of data.
    /// </summary>
    private int _frameLength;

    /// <summary>
    /// The current operating mode.
    /// </summary>
    private OperatingMode _mode;

    /// <inheritdoc cref="_sampleRate" />
    public int SampleRate {
        get => _sampleRate;
        set {
            if (!ValidateRateAndFrameLength(value, _frameLength)) {
                throw new InvalidOperationException("Invalid sample rate");
            }

            _sampleRate = value;
        }
    }

    /// <inheritdoc cref="_frameLength" />
    public int FrameLength {
        get => _frameLength;
        set {
            if (!ValidateRateAndFrameLength(_sampleRate, value)) {
                throw new InvalidOperationException("Invalid frame length");
            }

            _frameLength = value;
        }
    }

    /// <inheritdoc cref="_mode" />
    public OperatingMode OperatingMode {
        get => _mode;
        set {
            var result = NativeMethods.Vad_SetMode(_handle, (int) value);
            if (result != 0) {
                throw new InvalidOperationException("Invalid operating mode specified");
            }

            _mode = value;
        }
    }

    public WebRtcVad() {
        _sampleRate = 48000;
        _frameLength = 20;

        _mode = OperatingMode.HighQuality;
        
        _handle = NativeMethods.Vad_Create();
        var result = NativeMethods.Vad_Init(_handle);
        if (result != 0) {
            throw new InvalidOperationException("Could not initialize WebRtcVad");
        }
    }

    /// <summary>
    /// Whether the given audio frame contains speech.
    /// </summary>
    /// <param name="audioFrame">An array of shorts containing microphone input data.</param>
    /// <returns>True if the frame contains speech, otherwise false.</returns>
    public bool HasSpeech(short[] audioFrame) {
        return HasSpeech(audioFrame, _sampleRate, _frameLength);
    }

    /// <summary>
    /// Unsafe method for testing whether the given audio frame, with the given sample rate and frame length contains
    /// speech.
    /// </summary>
    /// <param name="audioFrame">An array of shorts containing microphone input data.</param>
    /// <param name="sampleRate">The sample rate of the data.</param>
    /// <param name="frameLength">The frame length of the data.</param>
    /// <returns></returns>
    private unsafe bool HasSpeech(short[] audioFrame, int sampleRate, int frameLength) {
        var samples = CalculateSamples(sampleRate, frameLength);

        int result;
        fixed (short* framePtr = audioFrame) {
            result = NativeMethods.Vad_Process(_handle, sampleRate, (IntPtr) framePtr, (UIntPtr) samples);
        }

        return result == 1;
    }

    /// <summary>
    /// Validate whether the sample rate and frame length are valid. Tests whether this library can work with the
    /// given sample rate and frame length.
    /// </summary>
    /// <returns>True if the sample rate and frame length are valid, otherwise false.</returns>
    private bool ValidateRateAndFrameLength(int sampleRate, int frameLength) {
        var samples = CalculateSamples(sampleRate, frameLength);
        
        return NativeMethods.Vad_ValidRateAndFrameLength(sampleRate, (UIntPtr) samples) == 0;
    }

    private static int CalculateSamples(int sampleRate, int frameLength) {
        return sampleRate / 1000 * frameLength;
    }

    private bool _disposed;

    public void Dispose() {
        if (_disposed) {
            return;
        }

        if (_handle != IntPtr.Zero) {
            NativeMethods.Vad_Free(_handle);
            _handle = IntPtr.Zero;
        }

        _disposed = true;
    }
}