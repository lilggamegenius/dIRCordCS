using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace dIRCordCS.Commands{
	public interface ICommand{
		Task HandleCommand(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e);
		Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e);
		Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e);
		Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e);
	}

	public class Commands{
		public Commands()=>commands = new Dictionary<string, ICommand>();
		public Dictionary<string, ICommand> commands{get;}

		public ICommand this[string i]{
			get=>commands[i.ToLower()];
			set=>commands[i.ToLower()] = value;
		}

		public bool ContainsCommand(string command)=>commands.ContainsKey(command.ToLower());
	}
}
