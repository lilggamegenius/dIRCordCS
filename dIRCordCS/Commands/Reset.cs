using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace dIRCordCS.Commands;

public class Reset : ICommand{
	internal const string Usage = "Usage: <botname> whois <username>";
	internal const string Epilogue = "This command has no options";

	static Reset()=>Bridge.RegisterCommand(nameof(Reset), new Reset());
	public async Task HandleCommand(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){
		IrcUser user = e.PrivateMessage.User;
		if(user.Hostmask.MatchHostMask(listener.Config.IRCBotOwnerHostmask)){
			ResetBot($"Reset requested by bot owner ({user.Hostmask}) via {nameof(Reset)} command");
		}

		await Bridge.Respond("This can only be run by the bot owner", channel, user);
	}
	public async Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		if(member.Id == listener.Config.DiscordBotOwnerID){
			ResetBot($"Reset requested by bot owner {member.Username}#{member.Discriminator} ({member.GetHostMask()}) via {nameof(Reset)} command");
		}

		await Bridge.Respond("This can only be run by the bot owner", e.Channel, member);
	}
	public async Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e)=>await Bridge.Respond("This is used by the bot owner to reset the bot", channel);
	public async Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		await Bridge.Respond("This is used by the bot owner to reset the bot", e.Channel);
	}

	private static void ResetBot(string message)=>throw new ResetException(message); // Trigger the reset
}
