using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NMaier.GetOptNet;

namespace dIRCordCS.Commands{
	public class Whois : ICommand{
		static Whois(){
			Bridge.RegisterCommand(nameof(Whois), new Whois());
		}

		internal const string Usage = "Usage: <botname> whois <username>";
		internal const string Epilogue = "This command has no options";
		private static readonly ILog Logger = LogManager.GetLogger<Ison>();

		public async void HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
			WhoisOptions opts = new WhoisOptions();
			try{
				opts.Parse(args);
				string find = opts.Parameters[0];
				DiscordChannel channel = listener.Config.ChannelMapObj[ircChannel];
				DiscordMember match = await Bridge.SearchForDiscordUser(find, channel);
				if(match != null){
					UserStatus status = match.Presence.Status;
					string statusStr = status.ToString();
					if(status == UserStatus.DoNotDisturb){
						statusStr = "not wanting to be disturbed";
					}

					var roles = match.Roles.ToCommaSeperatedList();
					await Bridge.Respond($"{find.ToBold()} is {match.GetHostMask().ToUnderline()} Status: {statusStr.ToItalics()} Currently {(match.Presence.Game.StreamType == GameStreamType.NoStream ? "playing" : "Streaming")}: {match.Presence.Game.Name.ToBold()} ({match.Presence.Game.Url})", ircChannel, e.PrivateMessage.User);
					await Bridge.Respond($"Registered: {match.CreationTimestamp:dddd, dd MMMM yyyy} Joined: {match.JoinedAt:dddd, dd MMMM yyyy} Avatar: {match.AvatarUrl}", ircChannel, e.PrivateMessage.User);
					if(roles.Length != 0)
						await Bridge.Respond($"Roles: {roles}", ircChannel, e.PrivateMessage.User, listener.ircClient, Bridge.MessageType.Notice);
				}
				else{
					await Bridge.Respond($"Unable to find a user by the name of {find}", ircChannel, e.PrivateMessage.User);
				}
			}
			catch(GetOptException){
				await Bridge.Respond(opts.AssembleUsage(Int32.MaxValue), ircChannel);
			}
			catch(Exception ex){
				Logger.Error($"Problem processing command: \n{ex}");
				await Bridge.Respond($"Sorry there was a problem processing the command: {ex.Message}", ircChannel);
			}
		}

		public void HandleCommand(DiscordListener listener, IList<string> args, MessageCreateEventArgs e){
			var user = e.Guild.GetMemberAsync(e.Author.Id);
			//IrcChannel channel = listener.Config.ChannelMapObj.Reverse[e.Channel];
			IsonOptions opts = new IsonOptions();
			opts.Parse(args);
			string find = opts.Parameters[0];
			IrcClient client = listener.Config.IrcClient;
			client.WhoIs(find,
			             async whois=>{
				             if(whois != null){
					             await Bridge.Respond($"{find}   is {whois.User.Hostmask}\n" +
					                                  $"{find}'s Real name:  {whois.User.RealName}\n" +
					                                  $"{find}'s Channels: {whois.Channels.ToCommaSeperatedList()}\n" +
					                                  $"{find}'s Server: {whois.Server}\n" +
					                                  $"{find}'s Idle time: {TimeSpan.FromSeconds(whois.SecondsIdle):g}"
					                                  , e.Channel, await user);
				             }
			             });
		}
	}

	[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Ignore, UsageIntro = Whois.Usage, UsageEpilog = Whois.Epilogue)]
	public class WhoisOptions : GetOpt{
		[Parameters(Min = 1)]
		public List<string> Parameters = new List<string>();
	}
}
