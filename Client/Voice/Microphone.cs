using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace SsmpVoiceChat.Client.Voice;

/// <summary>
/// Microphone class that handles the microphone device through OpenAL.
/// </summary>
public class Microphone {
    /// <summary>
    /// The device name of the microphone device as listed by OpenAL.
    /// </summary>
    private readonly string _deviceName;

    /// <summary>
    /// Integer pointer to the microphone device. Can be obtained from OpenAL and acts as a reference to pass back
    /// to OpenAL methods.
    /// </summary>
    private IntPtr _device;

    /// <summary>
    /// Whether the device is opened.
    /// </summary>
    public bool IsOpen => _device != IntPtr.Zero;
    /// <summary>
    /// Whether capture has started on the device.
    /// </summary>
    public bool IsStarted { get; private set; }

    public Microphone(string deviceName) {
        _device = IntPtr.Zero;
        _deviceName = deviceName;
    }

    /// <summary>
    /// Open the microphone device.
    /// </summary>
    /// <exception cref="Exception">Thrown if the microphone is already open.</exception>
    public void Open() {
        if (IsOpen) {
            throw new Exception("Microphone already open");
        }

        _device = OpenMic(_deviceName);
    }

    /// <summary>
    /// Start capturing the microphone input.
    /// </summary>
    public void Start() {
        if (!IsOpen) {
            return;
        }

        if (IsStarted) {
            return;
        }

        Alc.CaptureStart(_device);
        SoundManager.CheckAlcError(_device, 0);
        IsStarted = true;
    }

    /// <summary>
    /// Stop capturing the microphone input.
    /// </summary>
    public void Stop() {
        if (!IsOpen) {
            return;
        }

        if (!IsStarted) {
            return;
        }

        Alc.CaptureStop(_device);
        SoundManager.CheckAlcError(_device, 0);
        IsStarted = false;

        var available = Available();
        var buff = new short[available];
        var handle = GCHandle.Alloc(buff, GCHandleType.Pinned);

        try {
            Alc.CaptureSamples(_device, handle.AddrOfPinnedObject(), buff.Length);
            SoundManager.CheckAlcError(_device, 1);
        } catch (Exception e) {
            ClientVoiceChat.Logger.Error($"Exception while capturing samples:\n{e}");
        } finally {
            handle.Free();
        }
    }

    /// <summary>
    /// Close the microphone device.
    /// </summary>
    public void Close() {
        if (!IsOpen) {
            return;
        }

        Stop();
        Alc.CaptureCloseDevice(_device);
        SoundManager.CheckAlcError(_device, 0);
        _device = IntPtr.Zero;
    }

    /// <summary>
    /// Get the number of available capture samples from the microphone.
    /// </summary>
    /// <returns>The number of samples as an integer.</returns>
    public int Available() {
        Alc.GetInteger(_device, AlcGetInteger.CaptureSamples, 1, out var samples);
        SoundManager.CheckAlcError(_device, 0);

        return samples;
    }

    /// <summary>
    /// Read the captured microphone samples.
    /// </summary>
    /// <returns>A short array containing the samples.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the samples couldn't be read because there is not
    /// enough samples available.</exception>
    public short[] Read() {
        var available = Available();
        if (available < SoundManager.BufferSize) {
            throw new InvalidOperationException(
                $"Failed to read from microphone: Capacity {SoundManager.BufferSize}, available {available}");
        }

        var buff = new short[SoundManager.BufferSize];
        var handle = GCHandle.Alloc(buff, GCHandleType.Pinned);

        try {
            Alc.CaptureSamples(_device, handle.AddrOfPinnedObject(), buff.Length);
            SoundManager.CheckAlcError(_device, 0);
        } catch (Exception e) {
            ClientVoiceChat.Logger.Error($"Exception while capturing samples:\n{e}");
        } finally {
            handle.Free();
        }

        return buff;
    }

    /// <summary>
    /// Open the microphone device with the given name.
    /// </summary>
    /// <param name="name">The name of the device as specified by OpenAL.</param>
    /// <returns>An int pointer to the device.</returns>
    /// <exception cref="Exception">Thrown when neither the device with the given name nor the default microphone
    /// device could be opened.</exception>
    private IntPtr OpenMic(string name) {
        try {
            return TryOpenMic(name);
        } catch (Exception) {
            if (name != null) {
                ClientVoiceChat.Logger.Error($"Failed to open microphone '{name}', falling back to default microphone");
            }

            try {
                return TryOpenMic(GetDefaultMicrophone());
            } catch (Exception) {
                return TryOpenMic(null);
            }
        }
    }

    /// <summary>
    /// Try to open the microphone device with the given name. Will throw an exception if the device could not be
    /// opened.
    /// </summary>
    /// <param name="name">The name of the device as specified by OpenAL.</param>
    /// <returns>An int pointer to the device.</returns>
    /// <exception cref="Exception">Thrown when the device could not be opened.</exception>
    private IntPtr TryOpenMic(string name) {
        var device = Alc.CaptureOpenDevice(name, SoundManager.SampleRate, ALFormat.Mono16, SoundManager.BufferSize);
        if (device == IntPtr.Zero) {
            SoundManager.CheckAlcError(IntPtr.Zero, 0);
            throw new Exception("Failed to open microphone");
        }

        return device;
    }

    /// <summary>
    /// Get the name of the default microphone device from OpenAL.
    /// </summary>
    /// <returns>A string representing the default microphone device.</returns>
    public static string GetDefaultMicrophone() {
        var mic = Alc.GetString(IntPtr.Zero, AlcGetString.CaptureDefaultDeviceSpecifier);
        SoundManager.CheckAlcError(IntPtr.Zero, 0);

        return mic;
    }

    /// <summary>
    /// Get the names of all microphone devices from OpenAL.
    /// </summary>
    /// <returns>A list of strings for all the names of the microphone devices.</returns>
    public static List<string> GetAllMicrophones() {
        var devices = Alc.GetString(IntPtr.Zero, AlcGetStringList.CaptureDeviceSpecifier);
        SoundManager.CheckAlcError(IntPtr.Zero, 0);

        return devices == null ? [] : [..devices];
    }
}