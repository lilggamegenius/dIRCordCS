using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLog;
using NMaier.GetOptNet;

namespace dIRCordCS.Commands;

// ReSharper disable once UnusedMember.Global
// Loaded via reflection
public class Whois : ICommand{
	internal const string Usage = "Usage: <botname> whois <username>";
	internal const string Epilogue = "This command has no options";
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	static Whois()=>Bridge.RegisterCommand(nameof(Whois), new Whois());

	public async Task HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
		var opts = new WhoisOptions();
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

				string roles = match.Roles.ToCommaSeperatedList();
				await Bridge.Respond($"{find.ToBold()} is {match.GetHostMask().ToUnderline()} " +
									 $"Status: {statusStr.ToItalics()} Currently {DisplayActivity(match.Presence.Activity)})",
									 ircChannel,
									 e.PrivateMessage.User);
				await Bridge.Respond($"Registered: {match.CreationTimestamp:dddd, dd MMMM yyyy} Joined: {match.JoinedAt:dddd, dd MMMM yyyy} Avatar: {match.AvatarUrl}",
									 ircChannel,
									 e.PrivateMessage.User);
				if(roles.Length != 0){
					await Bridge.Respond($"Roles: {roles}", ircChannel, e.PrivateMessage.User, listener.IrcClient, Bridge.MessageType.Notice);
				}
			} else{
				await Bridge.Respond($"Unable to find a user by the name of {find}", ircChannel, e.PrivateMessage.User);
			}
		} catch(GetOptException){
			await Help(listener, ircChannel, args, e);
		}
	}

	public async Task HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		var opts = new IsonOptions();
		try{
			opts.Parse(args);
			string find = opts.Parameters[0];
			IrcClient client = listener.Config.IrcClient;
			client.WhoIs(find, WhoIsCallback);
			// return
			async void WhoIsCallback(WhoIs whois){
				if(whois == null) return;
				var response = new StringBuilder();
				response.AppendLine($"{find} is {whois.User.Hostmask}");
				response.AppendLine($"{find}'s Real name:  {whois.User.RealName}");
				if(whois.Channels.Length != 0){
					response.AppendLine($"{find}'s Channels: {whois.Channels.ToCommaSeperatedList()}");
				}

				response.AppendLine($"{find}'s Server: {whois.Server}");
				response.AppendLine($"{find}'s Idle time: {TimeSpan.FromSeconds(whois.SecondsIdle):g}");
				await Bridge.Respond(response.ToString(),
									 e.Channel,
									 member);
			}
		} catch(GetOptException){
			await Help(listener, member, args, e);
		}
	}

	public async Task Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e)=>await Bridge.Respond(new WhoisOptions().AssembleUsage(int.MaxValue), channel);

	public async Task Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
		await Bridge.Respond(new WhoisOptions().AssembleUsage(int.MaxValue), e.Channel);
	}

	private static string DisplayActivity(DiscordActivity activity){
		var status = new StringBuilder();
		switch(activity.ActivityType){
			case ActivityType.Custom:
				DiscordCustomStatus customStatus = activity.CustomStatus;
				return $"{customStatus.Name} {(customStatus.Emoji.RequiresColons ? customStatus.Emoji.Url : customStatus.Emoji.Name)}";
			case ActivityType.ListeningTo:
				status.Append("Listening to");
				break;
			case ActivityType.Competing:
				status.Append("Competing in");
				break;
			case ActivityType.Playing:
			case ActivityType.Streaming:
			case ActivityType.Watching:
			default:
				status.Append(nameof(activity.ActivityType));
				break;
		}

		status.Append(' ').Append(activity.Name);
		if(activity.StreamUrl != null){
			status.Append(" URL: ").Append(activity.StreamUrl);
		}

		return status.ToString();
	}
}
[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Ignore, UsageIntro = Whois.Usage, UsageEpilog = Whois.Epilogue)]
public class WhoisOptions : GetOpt{
	[Parameters(Min = 1)] public List<string> Parameters = new();
}
