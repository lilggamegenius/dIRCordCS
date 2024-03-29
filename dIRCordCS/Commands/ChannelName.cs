using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLog;

namespace dIRCordCS.Commands;

// ReSharper disable once UnusedMember.Global
// Loaded via reflection
public class LinkedChannel : ICommand{
	private const string Usage = "Usage: <botname> linkedChannel";
	internal const string Epilogue = "This command has no options";
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	static LinkedChannel()=>Bridge.RegisterCommand(nameof(LinkedChannel), new LinkedChannel());

	public async Task HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
		DiscordChannel channel = listener.Config.ChannelMapObj[ircChannel];
		await Bridge.Respond($"Linked channel is #{channel.Name} on server {channel.Guild.Name}", e.PrivateMessage.Channel, e.PrivateMessage.User);
	}

	public async Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		IrcChannel channel = listener.Config.ChannelMapObj.Reverse[e.Channel];
		await Bridge.Respond($"Linked channel is {channel.Name} on server {channel.Client.ServerInfo.NetworkName}", e.Channel, member);
	}

	public async Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e)=>await Bridge.Respond(Usage, channel);

	public async Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e)=>await Bridge.Respond(Usage, e.Channel);
}
