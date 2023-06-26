using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLog;
using NMaier.GetOptNet;

namespace dIRCordCS.Commands;

// ReSharper disable once UnusedMember.Global
// Loaded via reflection
public class Ison : ICommand{
	internal const string Usage = "Usage: <botname> ison <username>";
	internal const string Epilogue = "This command has no options";
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	static Ison()=>Bridge.RegisterCommand(nameof(Ison), new Ison());

	public async Task HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
		var opts = new IsonOptions();
		try{
			opts.Parse(args);
			string find = opts.Parameters[0];
			DiscordChannel channel = listener.Config.ChannelMapObj[ircChannel];
			DiscordMember match = (await Bridge.SearchForDiscordUser(find, channel)).Item1;
			if(match != null){
				UserStatus status = match.Presence.Status;
				string statusStr = status.ToString();
				if(status == UserStatus.DoNotDisturb){
					statusStr = "not wanting to be disturbed";
				}

				await Bridge.Respond($"{find} is currently {statusStr}", ircChannel, e.PrivateMessage.User);
			} else{
				await Bridge.Respond($"Unable to find a user by the name of {find}", ircChannel, e.PrivateMessage.User);
			}
		} catch(GetOptException){
			_ = Help(listener, ircChannel, args, e);
		}
	}

	public async Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		//Task<DiscordMember> user = e.Guild.GetMemberAsync(e.Author.Id);
		IrcChannel channel = listener.Config.ChannelMapObj.Reverse[e.Channel];
		var opts = new IsonOptions();
		try{
			opts.Parse(args);
			string find = opts.Parameters[0];
			IrcUser match = (await Bridge.SearchForIRCUser(find, channel, listener.Config.IrcListener)).Item1;
			if(match != null){
				await Bridge.Respond($"{match.Nick} is online", e.Channel, member);
			} else{
				await Bridge.Respond($"{find} was not found or is offline", e.Channel, member);
			}
		} catch(GetOptException){
			_ = Help(listener, member, args, e);
		}
	}

	public async Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e)=>await Bridge.Respond(new IsonOptions().AssembleUsage(int.MaxValue), channel);

	public async Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		await Bridge.Respond(new IsonOptions().AssembleUsage(int.MaxValue), e.Channel);
	}
}
[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Ignore, UsageIntro = Ison.Usage, UsageEpilog = Ison.Epilogue)]
public class IsonOptions : GetOpt{
	[Parameters(Min = 1)] public List<string> Parameters = new();
}
