using System;
using System.Collections.Generic;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NMaier.GetOptNet;

namespace dIRCordCS.Commands{
	// ReSharper disable once UnusedMember.Global
	// Loaded via reflection
	public class Whois : ICommand{
		internal const string Usage = "Usage: <botname> whois <username>";
		internal const string Epilogue = "This command has no options";
		private static readonly ILog Logger = LogManager.GetLogger<Ison>();
		static Whois(){Bridge.RegisterCommand(nameof(Whois), new Whois());}

		public async void HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
			WhoisOptions opts = new WhoisOptions();
			try{
				opts.Parse(args);
				string find = opts.Parameters[0];
				DiscordChannel channel = listener.Config.ChannelMapObj[ircChannel];
				DiscordMember match = (await Bridge.SearchForDiscordUser(find, channel)).Item1;
				if(match != null){
					UserStatus status = match.Presence.Status;
					string statusStr = status.ToString();
					if(status == UserStatus.DoNotDisturb){ statusStr = "not wanting to be disturbed"; }

					string roles = match.Roles.ToCommaSeperatedList();
					await
						Bridge.Respond($"{find.ToBold()} is {match.GetHostMask().ToUnderline()} Status: {statusStr.ToItalics()} Currently {(match.Presence.Game.StreamType == GameStreamType.NoStream ? "playing" : "Streaming")}: {match.Presence.Game.Name.ToBold()} ({match.Presence.Game.Url})",
									   ircChannel,
									   e.PrivateMessage.User);
					await Bridge.Respond($"Registered: {match.CreationTimestamp:dddd, dd MMMM yyyy} Joined: {match.JoinedAt:dddd, dd MMMM yyyy} Avatar: {match.AvatarUrl}",
										 ircChannel,
										 e.PrivateMessage.User);
					if(roles.Length != 0){ await Bridge.Respond($"Roles: {roles}", ircChannel, e.PrivateMessage.User, listener.IrcClient, Bridge.MessageType.Notice); }
				} else{ await Bridge.Respond($"Unable to find a user by the name of {find}", ircChannel, e.PrivateMessage.User); }
			} catch(GetOptException){ Help(listener, ircChannel, args, e); }
		}

		public async void HandleCommand(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
			IsonOptions opts = new IsonOptions();
			try{
				opts.Parse(args);
				string find = opts.Parameters[0];
				IrcClient client = listener.Config.IrcClient;
				client.WhoIs(find,
							 async whois=>{
								 if(whois != null){
									 await Bridge.Respond($"{find}   is {whois.User.Hostmask}\n"                          +
														  $"{find}'s Real name:  {whois.User.RealName}\n"                 +
														  $"{find}'s Channels: {whois.Channels.ToCommaSeperatedList()}\n" +
														  $"{find}'s Server: {whois.Server}\n"                            +
														  $"{find}'s Idle time: {TimeSpan.FromSeconds(whois.SecondsIdle):g}",
														  e.Channel,
														  member);
								 }
							 });
			} catch(GetOptException){ Help(listener, member, args, e); }
		}

		public async void Help(IrcListener listener, IrcChannel channel, IList<string> args, PrivateMessageEventArgs e){
			await Bridge.Respond(new WhoisOptions().AssembleUsage(int.MaxValue), channel);
		}

		public async void Help(DiscordListener listener, DiscordMember member, IList<string> args, MessageCreateEventArgs e){
			await Bridge.Respond(new WhoisOptions().AssembleUsage(int.MaxValue), e.Channel);
		}
	}

	[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Ignore, UsageIntro = Whois.Usage, UsageEpilog = Whois.Epilogue)]
	public class WhoisOptions : GetOpt{
		[Parameters(Min = 1)] public List<string> Parameters = new List<string>();
	}
}
