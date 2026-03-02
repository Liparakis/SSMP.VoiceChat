using System.Collections.Generic;
using SSMP.Api.Command;
using SSMP.Api.Command.Server;
using SsmpVoiceChat.Common.Command;

namespace SsmpVoiceChat.Server;

/// <summary>
/// Command for the server-side voice chat.
/// </summary>
public class ServerVoiceChatCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/voicechatserver";

    /// <inheritdoc />
    public string[] Aliases => ["/vcs"];

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// The server settings instance for reading and modifying values.
    /// </summary>
    private readonly ServerSettings _settings;

    /// <summary>
    /// Set of player IDs for players that are broadcasting their voice. This is a reference to the set from
    /// <see cref="ServerVoiceChat"/>: <see cref="ServerVoiceChat._broadcasters"/>.
    /// </summary>
    private readonly HashSet<ushort> _broadcasters;

    public ServerVoiceChatCommand(ServerSettings settings, HashSet<ushort> broadcasters) {
        _settings = settings;
        _broadcasters = broadcasters;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        void SendUsage() {
            commandSender.SendMessage($"Invalid usage: {Trigger} <set|broadcast>");
        }

        if (args.Length < 2) {
            SendUsage();
            return;
        }

        var action = args[1];
        if (action == "set") {
            HandleSet(commandSender, args);
        } else if (action == "broadcast") {
            HandleBroadcast(commandSender);
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Handle the set sub-command.
    /// </summary>
    /// <param name="commandSender">The command sender that executed this command.</param>
    /// <param name="args">A string array containing the arguments for this command. The first argument is
    /// the command trigger or alias.</param>
    private void HandleSet(ICommandSender commandSender, string[] args) {
        CommandUtil.HandleSetCommand(
            Trigger,
            args,
            _settings,
            commandSender.SendMessage,
            () => _settings.SaveToFile()
        );
    }

    /// <summary>
    /// Handle the broadcast sub-command.
    /// </summary>
    /// <param name="commandSender"></param>
    private void HandleBroadcast(ICommandSender commandSender) {
        if (commandSender is not IPlayerCommandSender player) {
            commandSender.SendMessage("Cannot execute this command as a non-player");
            return;
        }

        var id = player.Id;
        if (_broadcasters.Contains(id)) {
            _broadcasters.Remove(id);
            
            player.SendMessage("You are no longer broadcasting your voice");
        } else {
            _broadcasters.Add(id);
            
            player.SendMessage("You are now broadcasting your voice");
        }
    }
}