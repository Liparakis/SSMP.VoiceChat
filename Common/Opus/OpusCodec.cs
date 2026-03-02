using System;
using System.Collections.Generic;

namespace SsmpVoiceChat.Common.Opus;

/// <summary>
/// Class for the Opus codec for encoding and decoding voice data.
/// </summary>
public class OpusCodec {
    /// <summary>
    /// Decoder instance.
    /// </summary>
    private readonly OpusDecoder _decoder;
    /// <summary>
    /// Encoder instance.
    /// </summary>
    private readonly OpusEncoder _encoder;
    /// <summary>
    /// Sample rate in Hertz of data for both the encoder and decoder.
    /// </summary>
    private readonly int _sampleRate;
    /// <summary>
    /// Frame size in samples of data for both the encoder and decoder.
    /// </summary>
    private readonly ushort _frameSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpusCodec"/> class.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hertz (samples per second).</param>
    /// <param name="channels">The sample channels (1 for mono, 2 for stereo).</param>
    /// <param name="frameSize">Size of the frame in samples.</param>
    public OpusCodec(
        int sampleRate = Constants.DefaultAudioSampleRate,
        byte channels = Constants.DefaultAudioSampleChannels,
        ushort frameSize = Constants.DefaultAudioFrameSize
    ) {
        _sampleRate = sampleRate;
        _frameSize = frameSize;
        _decoder = new OpusDecoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
        _encoder = new OpusEncoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
    }

    /// <summary>
    /// Decode the given byte array of data.
    /// </summary>
    /// <param name="encodedData">Byte array containing encoded data.</param>
    /// <returns>A byte array of the decoded data.</returns>
    public byte[] Decode(byte[] encodedData) {
        if (encodedData == null) {
            _decoder.Decode(null, 0, 0, new byte[_sampleRate / _frameSize], 0);
            return null;
        }

        var samples = OpusDecoder.GetSamples(encodedData, 0, encodedData.Length, _sampleRate);
        if (samples < 1)
            return null;

        var dst = new byte[samples * sizeof(ushort)];
        var length = _decoder.Decode(encodedData, 0, encodedData.Length, dst, 0);
        if (dst.Length != length)
            Array.Resize(ref dst, length);
        return dst;
    }

    /// <summary>
    /// Encode the given byte array of data.
    /// </summary>
    /// <param name="data">Byte array containing raw data.</param>
    /// <returns>A byte array of the encoded data.</returns>
    public byte[] Encode(byte[] data) {
        var samples = data.Length / sizeof(ushort);
        var numberOfBytes = _encoder.FrameSizeInBytes(samples);

        var dst = new byte[numberOfBytes];
        var encodedBytes = _encoder.Encode(data, 0, dst, 0, samples);

        Array.Resize(ref dst, encodedBytes);

        return dst;
    }
}