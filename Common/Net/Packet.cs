using System;
using SSMP.Networking.Packet;

namespace SsmpVoiceChat.Common.Net;

/// <summary>
/// Packet for server-bound voice data.
/// </summary>
public class ServerVoicePacket : IPacketData {
    /// <summary>
    /// The maximum number of bytes that the <see cref="VoiceData"/> array can have.
    /// </summary>
    public const ushort MaxSize = ushort.MaxValue;

    /// <summary>
    /// The voice data as a byte array. This byte array should already be encoded using an encoder. It will be sent
    /// as is.
    /// </summary>
    public byte[] VoiceData { get; set; }
    
    /// <inheritdoc />
    public virtual void WriteData(IPacket packet) {
        if (VoiceData.Length > MaxSize) {
            throw new InvalidOperationException($"Voice data exceeds maximum size of {MaxSize} bytes");
        }

        var length = (ushort) VoiceData.Length;
        packet.Write(length);
        for (var i = 0; i < length; i++) {
            packet.Write(VoiceData[i]);
        }
    }

    /// <inheritdoc />
    public virtual void ReadData(IPacket packet) {
        var length = packet.ReadUShort();
        VoiceData = new byte[length];
        for (var i = 0; i < length; i++) {
            VoiceData[i] = packet.ReadByte();
        }
    }

    /// <inheritdoc />
    public bool IsReliable => false;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;
}

/// <summary>
/// Packet for client-bound voice data.
/// </summary>
public class ClientVoicePacket : ServerVoicePacket {
    /// <summary>
    /// The ID of the player from which this voice data is.
    /// </summary>
    public ushort Id { get; set; }

    /// <summary>
    /// Whether this voice data should be played with proximity-based volume.
    /// </summary>
    public bool Proximity { get; set; }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(Proximity);
        
        base.WriteData(packet);
    }
    
    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        Proximity = packet.ReadBool();
        
        base.ReadData(packet);
    }
}