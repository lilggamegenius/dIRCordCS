using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLog;

namespace dIRCordCS.Commands;

// ReSharper disable once UnusedType.Global
// Loaded via reflection
public class Topic : ICommand{
	private const string Usage = "Gets the topic from the connected channel";
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	static Topic(){Bridge.RegisterCommand(nameof(Topic), new Topic());}

	public async Task HandleCommand(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){
		await Bridge.Respond(Bridge.GetChannel(listener, channel).Topic, channel, e.PrivateMessage.User);
	}
	public async Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		await Bridge.Respond(Bridge.GetChannel(listener, e.Channel).Topic, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id));
	}
	public async Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){await Bridge.Respond(Usage, channel, e.PrivateMessage.User);}
	public async Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		await Bridge.Respond(Usage, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id));
	}
}
