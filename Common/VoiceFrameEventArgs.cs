using System;

namespace SsmpVoiceChat.Common;

/// <summary>
/// Identifies where an observed PCM voice frame originated from.
/// </summary>
public enum VoiceFrameSource {
    LocalMicrophone,
    ClientReceived,
    ServerReceived
}

/// <summary>
/// Describes a PCM voice frame observed by downstream mods.
/// </summary>
/// <remarks>
/// <see cref="Pcm16Data"/> may reference a pooled buffer.
/// Subscribers must not retain it beyond the event callback.
/// </remarks>
public readonly struct VoiceFrameEventArgs {
    /// <summary>
    /// The source of the frame in the voice pipeline.
    /// </summary>
    public VoiceFrameSource Source { get; }

    /// <summary>
    /// The ID of the relevant player, if applicable.
    /// </summary>
    public ushort? PlayerId { get; }

    /// <summary>
    /// The PCM audio data as little-endian signed 16-bit samples.
    /// Do not retain this reference beyond the event callback.
    /// </summary>
    public ReadOnlyMemory<byte> Pcm16Data { get; }

    /// <summary>
    /// The sample rate of <see cref="Pcm16Data"/> in Hz.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// The number of channels in <see cref="Pcm16Data"/>.
    /// </summary>
    public byte Channels { get; }

    /// <summary>
    /// The duration of the frame in milliseconds.
    /// </summary>
    public int FrameDurationMs { get; }

    /// <summary>
    /// Whether the frame contains speech per the current VAD decision.
    /// </summary>
    public bool HasSpeech { get; }

    /// <summary>
    /// Whether the frame is routed through proximity playback.
    /// </summary>
    public bool IsProximityPlayback { get; }

    public VoiceFrameEventArgs(
        VoiceFrameSource source,
        ushort? playerId,
        ReadOnlyMemory<byte> pcm16Data,
        int sampleRate,
        byte channels,
        int frameDurationMs,
        bool hasSpeech,
        bool isProximityPlayback
    ) {
        if (sampleRate <= 0)      throw new ArgumentOutOfRangeException(nameof(sampleRate), "Must be positive.");
        if (channels == 0)        throw new ArgumentOutOfRangeException(nameof(channels), "Must be at least 1.");
        if (frameDurationMs <= 0) throw new ArgumentOutOfRangeException(nameof(frameDurationMs), "Must be positive.");

        Source             = source;
        PlayerId           = playerId;
        Pcm16Data          = pcm16Data;
        SampleRate         = sampleRate;
        Channels           = channels;
        FrameDurationMs    = frameDurationMs;
        HasSpeech          = hasSpeech;
        IsProximityPlayback = isProximityPlayback;
    }
}