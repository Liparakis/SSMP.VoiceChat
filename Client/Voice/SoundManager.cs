using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Audio.OpenAL;

namespace SsmpVoiceChat.Client.Voice;

/// <summary>
/// Class for managing sound. Getting and setting the device speaker and creating speakers (<see cref="Speaker"/>) for
/// playing audio of other players.
/// </summary>
public class SoundManager {
    /// <summary>
    /// The sample rate for capturing microphone data and the sample rate of the audio data that is sent to speakers.
    /// </summary>
    public const int SampleRate = 48000;
    /// <summary>
    /// The length of a frame for audio data.
    /// </summary>
    public const int FrameLength = 20;
    /// <summary>
    /// The size of buffers that contain voice data. Based on sample rate and frame length.
    /// </summary>
    public const int BufferSize = SampleRate / 1000 * FrameLength;

    /// <summary>
    /// Int pointer to the device speaker from OpenAL.
    /// </summary>
    private IntPtr _device;
    /// <summary>
    /// The handle to the context in which the devices are created.
    /// </summary>
    private ContextHandle _context;

    /// <summary>
    /// Whether the device speaker is closed.
    /// </summary>
    private bool IsClosed => _device == IntPtr.Zero;
    
    /// <summary>
    /// Concurrent dictionary mapping player IDs to <see cref="Speaker"/>s.
    /// </summary>
    private readonly ConcurrentDictionary<ushort, Speaker> _speakers;

    public SoundManager() {
        _speakers = new ConcurrentDictionary<ushort, Speaker>();
    }

    /// <summary>
    /// Open the sound manager by getting the device speaker, opening it, and creating the context.
    /// </summary>
    public void Open() {
        string device;
        if (string.IsNullOrEmpty(VoiceChatMod.ModSettings.SpeakerDeviceName)) {
            device = GetDefaultDeviceSpeaker();
        } else {
            device = VoiceChatMod.ModSettings.SpeakerDeviceName;
        }

        _device = OpenDeviceSpeaker(device);
        _context = Alc.CreateContext(_device, []);

        Alc.MakeContextCurrent(_context);
    }

    /// <summary>
    /// Close the sound manager by closing all speakers, destroying the context, and closing the device speaker.
    /// </summary>
    public void Close() {
        foreach (var speaker in _speakers.Values) {
            speaker.Close();
        }

        _speakers.Clear();

        if (_context != ContextHandle.Zero) {
            Alc.DestroyContext(_context);
            CheckAlcError(_device, 0);
        }

        if (_device != IntPtr.Zero) {
            Alc.CloseDevice(_device);
        }

        _context = ContextHandle.Zero;
        _device = IntPtr.Zero;
    }

    /// <summary>
    /// Try to get or otherwise create a speaker for playing audio from a player.
    /// </summary>
    /// <param name="id">The ID to associate with the speaker, most likely the player ID.</param>
    /// <param name="speaker">If this method returns true, the found or created speaker.</param>
    /// <returns>True if a speaker was found or a new speaker was created, otherwise false.</returns>
    public bool TryGetOrCreateSpeaker(ushort id, out Speaker speaker) {
        if (IsClosed) {
            speaker = null;
            return false;
        }

        if (!_speakers.TryGetValue(id, out speaker)) {
            speaker = new Speaker();
            speaker.Open();

            _speakers.TryAdd(id, speaker);
        }

        return true;
    }

    /// <summary>
    /// Try to remove the speaker with the given ID.
    /// </summary>
    /// <param name="id">The ID for the speaker, most likely a player ID.</param>
    /// <returns>True if the speaker was removed, otherwise false.</returns>
    public bool TryRemoveSpeaker(ushort id) {
        if (IsClosed) {
            return false;
        }

        if (_speakers.TryRemove(id, out var speaker)) {
            speaker.Close();
        }

        return true;
    }

    /// <summary>
    /// Open the device speaker with the given name.
    /// </summary>
    /// <param name="name">The name of the device as specified by OpenAL.</param>
    /// <returns>An int pointer that represents the device in the context of OpenAL.</returns>
    /// <exception cref="Exception">Thrown if neither the device with the given name, nor the default device could be
    /// opened.</exception>
    private IntPtr OpenDeviceSpeaker(string name) {
        try {
            return TryOpenDeviceSpeaker(name);
        } catch (Exception) {
            if (name != null) {
                ClientVoiceChat.Logger.Error($"Failed to open audio channel '{name}', falling back to default");
            }

            try {
                return TryOpenDeviceSpeaker(GetDefaultDeviceSpeaker());
            } catch (Exception) {
                return TryOpenDeviceSpeaker(null);
            }
        }
    }

    /// <summary>
    /// Try to open the device speaker with the given name. Will throw an exception if the device could not be opened.
    /// </summary>
    /// <param name="name">The name of the device as specified by OpenAL.</param>
    /// <returns>An int pointer to the device.</returns>
    /// <exception cref="Exception">Thrown when the device could not be opened.</exception>
    private IntPtr TryOpenDeviceSpeaker(string name) {
        var device = Alc.OpenDevice(name);
        if (device == IntPtr.Zero) {
            throw new Exception("Failed to open audio device: Audio device not found");
        }

        CheckAlcError(device, 0);
        return device;
    }

    /// <summary>
    /// Get the name of the default device speaker from OpenAL.
    /// </summary>
    /// <returns>A string representing the default speaker device.</returns>
    public static string GetDefaultDeviceSpeaker() {
        var defaultSpeaker = Alc.GetString(IntPtr.Zero, AlcGetString.DefaultDeviceSpecifier);
        CheckAlcError(IntPtr.Zero, 0);

        return defaultSpeaker;
    }

    /// <summary>
    /// Get the names of all speaker devices from OpenAL.
    /// </summary>
    /// <returns>A list of strings for all the names of the speaker devices.</returns>
    public static List<string> GetAllDeviceSpeakers() {
        var devices = Alc.GetString(IntPtr.Zero, AlcGetStringList.AllDevicesSpecifier);
        CheckAlcError(IntPtr.Zero, 0);

        return devices == null ? [] : [..devices];
    }

    /// <summary>
    /// Check the error for AL.
    /// </summary>
    /// <param name="index">Index used as reference in the error message.</param>
    /// <returns>True if there was an error, otherwise false.</returns>
    public static bool CheckAlError(int index) {
        var error = AL.GetError();
        if (error == ALError.NoError) {
            return false;
        }

        var stackFrame = new StackFrame(1);
        ClientVoiceChat.Logger.Error(
            $"VoiceChat sound manager AL error: {stackFrame.GetMethod().DeclaringType}.{stackFrame.GetMethod().Name}[{index}] {error}");

        return true;
    }

    /// <summary>
    /// Check the error for ALC.
    /// </summary>
    /// <param name="index">Index used as reference in the error message.</param>
    /// <returns>True if there was an error, otherwise false.</returns>
    public static bool CheckAlcError(IntPtr device, int index) {
        var error = Alc.GetError(device);
        if (error == AlcError.NoError) {
            return false;
        }

        var stackFrame = new StackFrame(1);
        ClientVoiceChat.Logger.Error(
            $"VoiceChat sound manager ALC error: {stackFrame.GetMethod().DeclaringType}.{stackFrame.GetMethod().Name}[{index}] {error}");

        return true;
    }
}