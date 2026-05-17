using System;
using System.Threading;

namespace SsmpVoiceChat.Common;

/// <summary>
/// Public event hub for observing PCM voice frames without coupling to internal voice chat objects.
/// </summary>
public static class VoiceChatEvents
{
    // Copy-on-write: volatile so reads on the hot path never need a lock.
    // Subscribe/unsubscribe are rare — they can afford the lock + allocation.
    private static volatile EventHandler<VoiceFrameEventArgs>[] _voiceFrameHandlers = [];

    /// <summary>
    /// Synchronizes copy-on-write updates to <see cref="_voiceFrameHandlers"/> during subscription changes.
    /// </summary>
    private static readonly object Sync = new();

    /// <summary>
    /// Raised whenever the voice chat pipeline observes a PCM voice frame.
    /// </summary>
    public static event EventHandler<VoiceFrameEventArgs> VoiceFrameObserved
    {
        add
        {
            lock (Sync)
            {
                var current = _voiceFrameHandlers;
                var updated = new EventHandler<VoiceFrameEventArgs>[current.Length + 1];
                current.CopyTo(updated, 0);
                updated[current.Length] = value;
                _voiceFrameHandlers = updated;
            }
        }
        remove
        {
            lock (Sync)
            {
                var current = _voiceFrameHandlers;
                var index = Array.IndexOf(current, value);
                
                if (index < 0) {
                    return;
                }

                var updated = new EventHandler<VoiceFrameEventArgs>[current.Length - 1];
                Array.Copy(current, 0, updated, 0, index);
                Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
                _voiceFrameHandlers = updated;
            }
        }
    }

    /// <summary>
    /// Whether any listeners are currently subscribed to <see cref="VoiceFrameObserved"/>.
    /// </summary>
    internal static bool HasVoiceFrameObservers
    {
        get
        {
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
            // ReSharper disable once InconsistentlySynchronizedField
            return Volatile.Read(ref _voiceFrameHandlers).Length > 0;
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
        }
    }

    /// <summary>
    /// Raises <see cref="VoiceFrameObserved"/>, invoking each handler in isolation.
    /// </summary>
    /// <param name="sender">The originating pipeline object.</param>
    /// <param name="args">The observed voice frame.</param>
    /// <param name="logError">Error sink for handler exceptions. Must not be null.</param>
    internal static void RaiseVoiceFrameObserved(object sender, in VoiceFrameEventArgs args, Action<string> logError)
    {
        if (logError == null) throw new ArgumentNullException(nameof(logError));

        // Volatile.Read signals this unsynchronized access is intentional.
        // The copy-on-write pattern in add/remove guarantees the snapshot is stable.
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
        // ReSharper disable once InconsistentlySynchronizedField
        var handlers = Volatile.Read(ref _voiceFrameHandlers);
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
        
        if (handlers.Length == 0) {
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler(sender, args);
            }
            catch (Exception exception)
            {
                logError($"Voice frame observer threw an exception:\n{exception}");
            }
        }
    }
}
