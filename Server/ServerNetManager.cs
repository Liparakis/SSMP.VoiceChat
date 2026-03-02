using System;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;
using SSMP.Networking.Packet;
using SSMP.Networking.Packet.Data;
using SsmpVoiceChat.Common.Net;
using ServerPacketId = SsmpVoiceChat.Common.Net.ServerPacketId;

namespace SsmpVoiceChat.Server;

/// <summary>
/// Class that manages server-side networking for VoiceChat.
/// </summary>
public class ServerNetManager {
    /// <summary>
    /// Event that is called when a player's voice data is received.
    /// </summary>
    public event Action<ushort, byte[]> VoiceEvent;

    /// <summary>
    /// The server network sender.
    /// </summary>
    private readonly IServerAddonNetworkSender<ClientPacketId> _netSender;

    /// <summary>
    /// Construct the server network manager with the given server addon and net server instance.
    /// </summary>
    /// <param name="addon">The ServerAddon instance.</param>
    /// <param name="netServer">The net server instance.</param>
    public ServerNetManager(ServerAddon addon, INetServer netServer) {
        _netSender = netServer.GetNetworkSender<ClientPacketId>(addon);

        var netReceiver = netServer.GetNetworkReceiver<ServerPacketId>(addon, InstantiatePacket);
        
        netReceiver.RegisterPacketHandler<ServerVoicePacket>(ServerPacketId.Voice, (id, packet) => {
            VoiceEvent?.Invoke(id, packet.VoiceData);
        });
    }

    /// <summary>
    /// Send voice data to the given receiver from the sender and mark whether it should be played positionally.
    /// </summary>
    /// <param name="receiver">The ID of the receiving player.</param>
    /// <param name="sender">The ID of the sending player.</param>
    /// <param name="data">The voice data.</param>
    /// <param name="proximity">Whether the audio should be played positionally.</param>
    public void SendVoiceData(ushort receiver, ushort sender, byte[] data, bool proximity) {
        if (data.Length > ServerVoicePacket.MaxSize) {
            ServerVoiceChat.Logger.Info("Voice data exceeds max size!");
            return;
        }
        
        _netSender.SendCollectionData(ClientPacketId.Voice, new ClientVoicePacket {
            Id = sender,
            Proximity = proximity,
            VoiceData = data
        }, receiver);
    }

    /// <summary>
    /// Function to instantiate IPacketData instances given a packet ID.
    /// </summary>
    /// <param name="packetId">The server packet ID.</param>
    /// <returns>An instance of IPacketData.</returns>
    private static IPacketData InstantiatePacket(ServerPacketId packetId) {
        switch (packetId) {
            case ServerPacketId.Voice:
                return new PacketDataCollection<ServerVoicePacket>();
        }

        return null;
    }
}