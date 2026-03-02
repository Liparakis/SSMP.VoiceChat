// 
// Author: John Carruthers (johnc@frag-labs.com)
// 
// Copyright (C) 2013 John Carruthers
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//  
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//  
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Linq;

namespace SsmpVoiceChat.Common.Opus;

/// <summary>
/// Opus encoder.
/// </summary>
public class OpusEncoder : IDisposable {
    /// <summary>
    /// Opus encoder.
    /// </summary>
    private IntPtr _encoder;

    /// <summary>
    /// Size of each sample in bytes.
    /// </summary>
    private readonly int _sampleSize;

    /// <summary>
    /// Permitted frame sizes in ms.
    /// </summary>
    private readonly float[] _permittedFrameSizesInMilliSec = [
        2.5f, 5, 10,
        20, 40, 60
    ];

    /// <summary>
    /// Creates a new Opus encoder.
    /// </summary>
    /// <param name="srcSamplingRate">The sampling rate of the input stream.</param>
    /// <param name="srcChannelCount">The number of channels in the input stream.</param>
    public OpusEncoder(int srcSamplingRate, int srcChannelCount) {
        if (srcSamplingRate != 8000 &&
            srcSamplingRate != 12000 &&
            srcSamplingRate != 16000 &&
            srcSamplingRate != 24000 &&
            srcSamplingRate != 48000)
            throw new ArgumentOutOfRangeException(nameof(srcSamplingRate));
        if (srcChannelCount != 1 && srcChannelCount != 2)
            throw new ArgumentOutOfRangeException(nameof(srcChannelCount));

        var encoder =
            NativeMethods.opus_encoder_create(srcSamplingRate, srcChannelCount, (int) Application.Voip, out var error);
        if ((NativeMethods.OpusErrors) error != NativeMethods.OpusErrors.Ok) {
            throw new Exception("Exception occured while creating encoder");
        }

        _encoder = encoder;

        const int bitDepth = 16;
        _sampleSize = SampleSize(bitDepth, srcChannelCount);

        PermittedFrameSizes = new int[_permittedFrameSizesInMilliSec.Length];
        for (var i = 0; i < _permittedFrameSizesInMilliSec.Length; i++)
            PermittedFrameSizes[i] = (int) (srcSamplingRate / 1000f * _permittedFrameSizesInMilliSec[i]);
    }

    private static int SampleSize(int bitDepth, int channelCount) {
        return bitDepth / 8 * channelCount;
    }

    /// <summary>
    /// Deconstructor for when this instance is garbage collected. Will dispose the object.
    /// </summary>
    ~OpusEncoder() {
        Dispose();
    }

    /// <summary>
    /// Encode audio samples.
    /// </summary>
    /// <param name="srcPcmSamples">PCM samples to be encoded.</param>
    /// <param name="srcOffset">The zero-based byte offset in srcPcmSamples at which to begin reading PCM samples.</param>
    /// <param name="dstOutputBuffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values starting at offset replaced with encoded audio data.</param>
    /// <param name="dstOffset">The zero-based byte offset in dstOutputBuffer at which to begin writing encoded audio.</param>
    /// <param name="sampleCount">The number of samples, per channel, to encode.</param>
    /// <returns>The total number of bytes written to dstOutputBuffer.</returns>
    public unsafe int Encode(byte[] srcPcmSamples, int srcOffset, byte[] dstOutputBuffer, int dstOffset,
        int sampleCount) {
        if (srcPcmSamples == null) throw new ArgumentNullException(nameof(srcPcmSamples));
        if (dstOutputBuffer == null) throw new ArgumentNullException(nameof(dstOutputBuffer));
        if (!PermittedFrameSizes.Contains(sampleCount))
            throw new Exception("Frame size is not permitted");
        var readSize = _sampleSize * sampleCount;
        if (srcOffset + readSize > srcPcmSamples.Length)
            throw new Exception("Not enough samples in source");
        var maxSizeBytes = dstOutputBuffer.Length - dstOffset;
        int encodedLen;
        fixed (byte* bEnc = dstOutputBuffer) {
            fixed (byte* bSrc = srcPcmSamples) {
                var encodedPtr = IntPtr.Add(new IntPtr(bEnc), dstOffset);
                var pcmPtr = IntPtr.Add(new IntPtr(bSrc), srcOffset);
                encodedLen = NativeMethods.opus_encode(_encoder, pcmPtr, sampleCount, encodedPtr, maxSizeBytes);
            }
        }

        if (encodedLen < 0)
            throw new Exception("Encoding failed - " + (NativeMethods.OpusErrors) encodedLen);
        return encodedLen;
    }

    /// <summary>
    /// Calculates the size of a frame in bytes.
    /// </summary>
    /// <param name="frameSizeInSamples">Size of the frame in samples per channel.</param>
    /// <returns>The size of a frame in bytes.</returns>
    public int FrameSizeInBytes(int frameSizeInSamples) {
        return frameSizeInSamples * _sampleSize;
    }

    /// <summary>
    /// Permitted frame sizes in samples per channel.
    /// </summary>
    public int[] PermittedFrameSizes { get; }

    /// <summary>
    /// Gets or sets the bitrate setting of the encoding.
    /// </summary>
    public int Bitrate {
        get {
            if (_encoder == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");
            var ret = NativeMethods.opus_encoder_ctl_out(_encoder, NativeMethods.Ctl.GetBitrateRequest,
                out var bitrate);
            if (ret < 0)
                throw new Exception("Encoder error - " + (NativeMethods.OpusErrors) ret);
            return bitrate;
        }
        set {
            if (_encoder == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");
            var ret = NativeMethods.opus_encoder_ctl(_encoder, NativeMethods.Ctl.SetBitrateRequest, value);
            if (ret < 0)
                throw new Exception("Encoder error - " + (NativeMethods.OpusErrors) ret);
        }
    }

    /// <summary>
    /// Gets or sets if Forward Error Correction encoding is enabled.
    /// </summary>
    public bool EnableForwardErrorCorrection {
        get {
            if (_encoder == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");
            var ret = NativeMethods.opus_encoder_ctl_out(_encoder, NativeMethods.Ctl.GetInbandFecRequest, out var fec);
            if (ret < 0)
                throw new Exception("Encoder error - " + (NativeMethods.OpusErrors) ret);
            return fec > 0;
        }
        set {
            if (_encoder == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");
            var ret = NativeMethods.opus_encoder_ctl(_encoder, NativeMethods.Ctl.SetInbandFecRequest,
                Convert.ToInt32(value));
            if (ret < 0)
                throw new Exception("Encoder error - " + (NativeMethods.OpusErrors) ret);
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public void Dispose() {
        if (_encoder != IntPtr.Zero) {
            NativeMethods.opus_encoder_destroy(_encoder);
            _encoder = IntPtr.Zero;
        }
    }
}