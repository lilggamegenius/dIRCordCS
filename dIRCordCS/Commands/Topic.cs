using System.Collections.Generic;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace dIRCordCS.Commands{
	// ReSharper disable once UnusedMember.Global
	// Loaded via reflection
	public class Topic : ICommand{
		static Topic(){Bridge.RegisterCommand(nameof(Topic), new Topic());}

		private const string help = "Gets the topic from the connected channel";
		private static readonly ILog Logger = LogManager.GetLogger<Ison>();

		public async void HandleCommand(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){
			await Bridge.Respond(Bridge.GetChannel(listener, channel).Topic, channel, e.PrivateMessage.User);
		}
		public async void HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
			await Bridge.Respond(Bridge.GetChannel(listener, e.Channel).Topic, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id));
		}
		public async void Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){await Bridge.Respond(help, channel, e.PrivateMessage.User);}
		public async void Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
			await Bridge.Respond(help, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id));
		}
	}
}
