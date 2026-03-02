namespace SsmpVoiceChat.Common.WebRtcVad; 

/// <summary>
/// Operating mode for Web RTC VAD.
/// </summary>
public enum OperatingMode {
    HighQuality = 0,
    LowBitrate = 1,
    Aggressive = 2,
    VeryAggressive = 3
}