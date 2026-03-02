using System;
using SSMP.Math;
using SsmpVoiceChat.Common;
using SsmpVoiceChat.Common.Opus;
using OpenTK.Audio.OpenAL;

namespace SsmpVoiceChat.Client.Voice;

/// <summary>
/// Class that represents a single audio source. Used for outputting voice data from another player.
/// </summary>
public class Speaker {
    /// <summary>
    /// The default maximum distance for proximity audio.
    /// </summary>
    private const float DefaultMaxDistance = 60f;
    /// <summary>
    /// The number of buffers to use for storing audio data for playback to the speaker.
    /// </summary>
    private const int NumBuffers = 32;

    /// <summary>
    /// Opus codec instance for decoding audio data for this speaker.
    /// </summary>
    private readonly OpusCodec _decoder;

    /// <summary>
    /// Integer representing the OpenAL source. Used as a reference to the source for use in OpenAL methods.
    /// </summary>
    private int _source;
    /// <summary>
    /// Array of integers representing the OpenAL buffers. Used as references to the buffers for use in OpenAL methods.
    /// </summary>
    private int[] _buffers;

    /// <summary>
    /// Index of the currently used buffer.
    /// </summary>
    private int _bufferIndex;

    public Speaker() {
        _decoder = new OpusCodec();
    }

    /// <summary>
    /// Open the speaker by generating the source and buffers, and setting parameters.
    /// </summary>
    public void Open() {
        if (HasValidSource()) {
            return;
        }

        _source = AL.GenSource();
        SoundManager.CheckAlError(0);
        AL.Source(_source, ALSourceb.Looping, false);
        SoundManager.CheckAlError(1);

        AL.DistanceModel(ALDistanceModel.LinearDistance);
        SoundManager.CheckAlError(2);
        AL.Source(_source, ALSourcef.MaxDistance, DefaultMaxDistance);
        SoundManager.CheckAlError(3);
        AL.Source(_source, ALSourcef.ReferenceDistance, 0f);
        SoundManager.CheckAlError(4);
        AL.Source(_source, ALSource3f.Direction, 0f, 0f, 0f);
        SoundManager.CheckAlError(5);

        _buffers = AL.GenBuffers(NumBuffers);
        SoundManager.CheckAlError(6);
    }

    /// <summary>
    /// Play the given encoded data, with the given volume, (optionally) at the given position, (optionally) with the
    /// given max distance.
    /// </summary>
    /// <param name="encodedData">The voice data encoded with Opus.</param>
    /// <param name="volume">The volume that the audio should play at.</param>
    /// <param name="position">The position at which the audio should play, or null if the audio should not be played
    /// positionally.</param>
    /// <param name="maxDistance">The maximum distance the audio should be heard from. If <paramref name="position"/>
    /// is supplied this max distance will determine the relative volume of the audio.</param>
    public void Play(byte[] encodedData, float volume = 1f, Vector3 position = null, float maxDistance = DefaultMaxDistance) {
        var byteData = _decoder.Decode(encodedData);
        var data = DataUtils.BytesToShorts(byteData);
        
        RemoveProcessedBuffers();

        Write(data, volume, position, maxDistance);

        var buffers = GetQueuedBuffers();
        var stopped = GetState() == ALSourceState.Initial || GetState() == ALSourceState.Stopped || buffers <= 1;

        if (stopped) {
            AL.SourcePlay(_source);
            SoundManager.CheckAlError(0);
        }
    }

    /// <summary>
    /// Write the given raw data to the source buffer for playback.
    /// </summary>
    /// <param name="data">The raw unencoded audio data.</param>
    /// <param name="volume">The volume of the audio.</param>
    /// <param name="position">The position at which the audio should play, or null if the audio should not be played
    /// positionally.</param>
    /// <param name="maxDistance">The maximum distance the audio should be heard from. If <paramref name="position"/>
    /// is supplied this max distance will determine the relative volume of the audio.</param>
    private void Write(short[] data, float volume, Vector3 position, float maxDistance) {
        SetPosition(position, maxDistance);

        AL.Source(_source, ALSourcef.MaxGain, 6f);
        SoundManager.CheckAlError(0);
        AL.Source(_source, ALSourcef.Gain, volume);
        SoundManager.CheckAlError(1);
        AL.Listener(ALListenerf.Gain, 1f);
        SoundManager.CheckAlError(2);

        var queuedBuffers = GetQueuedBuffers();
        if (queuedBuffers >= _buffers.Length) {
            AL.GetSource(_source, ALGetSourcei.SampleOffset, out var sampleOffset);
            SoundManager.CheckAlError(3);
            AL.Source(_source, ALSourcei.SampleOffset, sampleOffset + SoundManager.BufferSize);
            SoundManager.CheckAlError(4);

            RemoveProcessedBuffers();
        }

        AL.BufferData(_buffers[_bufferIndex], ALFormat.Mono16, data, data.Length * sizeof(short),
            SoundManager.SampleRate);
        SoundManager.CheckAlError(5);
        AL.SourceQueueBuffer(_source, _buffers[_bufferIndex]);
        SoundManager.CheckAlError(6);

        _bufferIndex = (_bufferIndex + 1) % _buffers.Length;
    }

    /// <summary>
    /// Set linear attenuation (linear volume drop-off) with the given max distance.
    /// </summary>
    /// <param name="maxDistance">The maximum distance as a float.</param>
    private void LinearAttenuation(float maxDistance) {
        AL.DistanceModel(ALDistanceModel.LinearDistance);
        SoundManager.CheckAlError(0);
        AL.Source(_source, ALSourcef.MaxDistance, maxDistance);
        SoundManager.CheckAlError(1);
    }

    /// <summary>
    /// Set the position of the audio source.
    /// </summary>
    /// <param name="soundPos">The position as a float vector.</param>
    /// <param name="maxDistance">The maximum distance for positionally played audio.</param>
    private void SetPosition(Vector3 soundPos, float maxDistance) {
        AL.Listener(ALListener3f.Position, 0f, 0f, 0f);
        SoundManager.CheckAlError(0);

        var orientation = new[] { 0f, 0f, -1f, 0f, 1f, 0f };
        AL.Listener(ALListenerfv.Orientation, ref orientation);
        SoundManager.CheckAlError(1);

        if (soundPos != null) {
            var x = soundPos.X;
            var y = soundPos.Y;
            var z = soundPos.Z;
            if (VoiceChatMod.ModSettings.SmoothChannelTransition && x is < 5f or > -5f) {
                z = (float) -Math.Sqrt(25f - Math.Pow(x, 2));
            }

            LinearAttenuation(maxDistance);
            AL.Source(_source, ALSourceb.SourceRelative, false);
            SoundManager.CheckAlError(2);
            AL.Source(_source, ALSource3f.Position, x, y, z);
            SoundManager.CheckAlError(3);
        } else {
            LinearAttenuation(DefaultMaxDistance);
            AL.Source(_source, ALSourceb.SourceRelative, true);
            SoundManager.CheckAlError(4);
            AL.Source(_source, ALSource3f.Position, 0f, 0f, 0f);
            SoundManager.CheckAlError(5);
        }
    }

    /// <summary>
    /// Close the speaker by stopping playback, un-queuing buffers and deleting the source and buffers.
    /// </summary>
    public void Close() {
        if (HasValidSource()) {
            if (GetState() == ALSourceState.Playing) {
                AL.SourceStop(_source);
                SoundManager.CheckAlError(0);
            }

            AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out var processed);
            SoundManager.CheckAlError(1);

            if (processed > 0) {
                AL.SourceUnqueueBuffers(_source, processed);
                SoundManager.CheckAlError(2);
            }

            AL.DeleteSource(_source);
            SoundManager.CheckAlError(3);

            AL.DeleteBuffers(_buffers);
            SoundManager.CheckAlError(4);
        }

        _source = 0;
    }

    /// <summary>
    /// Remove buffers that have been processed by the source.
    /// </summary>
    private void RemoveProcessedBuffers() {
        AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out var processed);
        SoundManager.CheckAlError(0);

        if (processed > 0) {
            AL.SourceUnqueueBuffers(_source, processed);
            SoundManager.CheckAlError(1);
        }
    }

    /// <summary>
    /// Get the state of the audio source.
    /// </summary>
    /// <returns></returns>
    private ALSourceState GetState() {
        AL.GetSource(_source, ALGetSourcei.SourceState, out var state);
        SoundManager.CheckAlError(0);

        return (ALSourceState) state;
    }

    /// <summary>
    /// Get the number of buffers that are queued in the audio source.
    /// </summary>
    /// <returns>The number of queued buffers as an integer.</returns>
    private int GetQueuedBuffers() {
        AL.GetSource(_source, ALGetSourcei.BuffersQueued, out var buffers);
        SoundManager.CheckAlError(0);

        return buffers;
    }

    /// <summary>
    /// Whether this speaker has a valid audio source.
    /// </summary>
    /// <returns>True if the source is valid, otherwise false.</returns>
    private bool HasValidSource() {
        var validSource = AL.IsSource(_source);

        return validSource;
    }
}