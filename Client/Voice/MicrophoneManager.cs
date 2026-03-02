using System;
using System.Threading;
using SsmpVoiceChat.Common;
using SsmpVoiceChat.Common.Opus;
using SsmpVoiceChat.Common.RNNoise;
using SsmpVoiceChat.Common.WebRtcVad;

namespace SsmpVoiceChat.Client.Voice;

/// <summary>
/// Class that manages the microphone. Creates the microphone device, handles reading from it, handles denoising,
/// encoding, and voice activation.
/// </summary>
public class MicrophoneManager {
    /// <summary>
    /// Event that is called whenever there is voice data available to be used.
    /// </summary>
    public event Action<byte[]> VoiceDataEvent;

    /// <summary>
    /// The Opus encoding for encoding voice data.
    /// </summary>
    private readonly OpusCodec _encoder;
    /// <summary>
    /// The denoiser to denoise voice data.
    /// </summary>
    private readonly RNNoise _denoiser;
    /// <summary>
    /// The WebRTC VAD instance to check whether there is voice data in the samples.
    /// </summary>
    private readonly WebRtcVad _webRtcVad;

    /// <summary>
    /// The thread that checks for new voice data from the microphone.
    /// </summary>
    private Thread _thread;
    /// <summary>
    /// Whether the thread (<see cref="_thread"/>) is running.
    /// </summary>
    private bool _isRunning;

    /// <summary>
    /// 
    /// </summary>
    private Microphone _microphone;

    /// <summary>
    /// Whether the microphone is 'activating', meaning that together with the last samples, the samples contain
    /// voice data.
    /// </summary>
    private bool _activating;
    /// <summary>
    /// The last buffer of voice data to check whether the microphone is activating.
    /// <seealso cref="_activating"/>
    /// </summary>
    private byte[] _lastBuff;

    public MicrophoneManager() {
        _encoder = new OpusCodec();
        _denoiser = new RNNoise();
        _webRtcVad = new WebRtcVad {
            SampleRate = SoundManager.SampleRate,
            FrameLength = SoundManager.FrameLength,
            OperatingMode = OperatingMode.Aggressive
        };
    }

    /// <summary>
    /// Start the thread that checks for and processes microphone voice data.
    /// </summary>
    public void Start() {
        if (_isRunning) {
            Stop();
        }

        _thread = new Thread(() => {
            _isRunning = true;

            if (!GetMic()) {
                return;
            }

            while (_isRunning) {
                try {
                    if (!PollMic(out var buff)) {
                        continue;
                    }

                    // Convert the mic data to bytes and check whether it contains speech with WebRTC VAD
                    var byteBuff = DataUtils.ShortsToBytes(buff);
                    var hasSpeech = _webRtcVad.HasSpeech(buff);

                    if (!_activating) {
                        if (hasSpeech) {
                            if (_lastBuff != null) {
                                VoiceDataEvent?.Invoke(_encoder.Encode(_lastBuff));
                            }
                            VoiceDataEvent?.Invoke(_encoder.Encode(byteBuff));
                            
                            _activating = true;
                            ClientVoiceChat.Logger.Debug("Mic buffer has speech, activating");
                        }
                    } else {
                        if (!hasSpeech) {
                            _activating = false;
                            
                            ClientVoiceChat.Logger.Debug("Mic buffer does not have speech, de-activating");
                        } else {
                            VoiceDataEvent?.Invoke(_encoder.Encode(byteBuff));
                        }
                    }

                    _lastBuff = byteBuff;
                } catch (Exception e) {
                    ClientVoiceChat.Logger.Error($"Error in mic thread:\n{e}");
                }
            }
        });
        _thread.Start();
    }

    /// <summary>
    /// Stop the thread that checks and processes microphone voice data.
    /// </summary>
    public void Stop() {
        if (!_isRunning) {
            return;
        }

        _isRunning = false;

        _thread.Join(100);
        _thread = null;

        _microphone.Close();
        _microphone = null;
    }

    /// <summary>
    /// Get the microphone for this instance based on the device name in the mod settings.
    /// Also opens the microphone if it is not yet open.
    /// </summary>
    /// <returns>True if the microphone was obtained, false otherwise.</returns>
    private bool GetMic() {
        if (!_isRunning) {
            return false;
        }

        _microphone ??= new Microphone(VoiceChatMod.ModSettings.MicrophoneDeviceName);

        if (!_microphone.IsOpen) {
            _microphone.Open();
        }

        return true;
    }

    /// <summary>
    /// Poll the microphone for new samples. Starts the microphone if it was not yet started. Sleeps the calling
    /// thread if there are no available samples. Microphone samples are amplified (according to settings) and
    /// denoised before being returned.
    /// </summary>
    /// <param name="buff">When this method returns true, contains the voice data as an array of shorts. Otherwise,
    /// undefined.</param>
    /// <returns>True if the microphone has new voice data samples, otherwise false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the microphone is null.</exception>
    private bool PollMic(out short[] buff) {
        if (_microphone == null) {
            throw new InvalidOperationException("Cannot poll unknown microphone");
        }

        if (!_microphone.IsStarted) {
            _microphone.Start();
        }
        
        if (_microphone.Available() < SoundManager.BufferSize) {
            Thread.Sleep(5);
            buff = null;
            return false;
        }

        buff = _microphone.Read();

        if (buff == null) {
            Thread.Sleep(5);
            buff = null;
            return false;
        }
                    
        // Adjust volume of mic data based on config value
        buff = VolumeManager.AmplifyAudioData(buff, VoiceChatMod.ModSettings.MicrophoneAmplification);

        // Denoise the mic data
        buff = _denoiser.ProcessFrame(buff);
        return true;
    }
}