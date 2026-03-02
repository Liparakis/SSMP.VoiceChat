using System;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;
using SSMP.Networking.Packet.Data;
using SSMPVoiceChat.Common.Net;
using ServerPacketId = SsmpVoiceChat.Common.Net.ServerPacketId;

namespace SsmpVoiceChat.Client;

/// <summary>
/// Class that manages client-side networking for VoiceChat.
/// </summary>
public class ClientNetManager {
    /// <summary>
    /// Event that is called when voice data is received.
    /// </summary>
    public event Action<ushort, byte[], bool> VoiceEvent;

    /// <summary>
    /// The client network sender.
    /// </summary>
    private readonly IClientAddonNetworkSender<ServerPacketId> _netSender;

    /// <summary>
    /// Construct the network manager with the given addon and net client.
    /// </summary>
    /// <param name="addon">The client addon for getting the network sender and receiver.</param>
    /// <param name="netClient">The net client interface for accessing network related methods.</param>
    public ClientNetManager(ClientAddon addon, INetClient netClient) {
        _netSender = netClient.GetNetworkSender<ServerPacketId>(addon);

        var netReceiver = netClient.GetNetworkReceiver<ClientPacketId>(addon, InstantiatePacket);

        netReceiver.RegisterPacketHandler<ClientVoicePacket>(ClientPacketId.Voice,
            packet => { VoiceEvent?.Invoke(packet.Id, packet.VoiceData, packet.Proximity); });
    }

    /// <summary>
    /// Send voice data from the local player to the server.
    /// </summary>
    public void SendVoiceData(byte[] data) {
        if (data.Length > ServerVoicePacket.MaxSize) {
            ClientVoiceChat.Logger.Error("Voice data exceeds max size!");
            return;
        }

        _netSender.SendCollectionData(ServerPacketId.Voice, new ServerVoicePacket {
            VoiceData = data
        });
    }

    /// <summary>
    /// Function to instantiate packet data instances given a packet ID.
    /// </summary>
    /// <param name="packetId">The client packet ID.</param>
    /// <returns>An instance of IPacketData.</returns>
    private static IPacketData InstantiatePacket(ClientPacketId packetId) {
        switch (packetId) {
            case ClientPacketId.Voice:
                return new PacketDataCollection<ClientVoicePacket>();
        }

        return null;
    }
}