using System.Collections.Generic;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace dIRCordCS.Commands{
	public interface ICommand{
		void HandleCommand(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e);
		void HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e);
		void Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e);
		void Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e);
	}

	public class Commands{
		public Dictionary<string, ICommand> commands{get;}

		public Commands(){commands = new Dictionary<string, ICommand>();}

		public ICommand this[string i]{
			get=>commands[i.ToLower()];
			set=>commands[i.ToLower()] = value;
		}

		public bool ContainsCommand(string command){return commands.ContainsKey(command.ToLower());}
	}
}
