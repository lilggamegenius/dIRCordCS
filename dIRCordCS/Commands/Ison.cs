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
	public class Ison : ICommand{
		static Ison(){
			Bridge.RegisterCommand(nameof(Ison), new Ison());
		}

		internal const string Usage = "Usage: <botname> ison <username>";
		internal const string Epilogue = "This command has no options";
		private static readonly ILog Logger = LogManager.GetLogger<Ison>();

		public async void HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
			IsonOptions opts = new IsonOptions();
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
					await Bridge.Respond($"{find} is currently {statusStr}", ircChannel, e.PrivateMessage.User);
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

		public async void HandleCommand(DiscordListener listener, IList<string> args, MessageCreateEventArgs e){
			var user = e.Guild.GetMemberAsync(e.Author.Id);
			IrcChannel channel = listener.Config.ChannelMapObj.Reverse[e.Channel];
			IsonOptions opts = new IsonOptions();
			opts.Parse(args);
			string find = opts.Parameters[0];
			IrcUser match = Bridge.SearchForIRCUser(find, channel, listener.Config.IrcListener);
			if(match != null){
				await Bridge.Respond($"{match.Nick} is online", e.Channel, await user);
			}
			else{
				await Bridge.Respond($"{find} was not found", e.Channel, await user);
			}
		}
	}

	[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Ignore, UsageIntro = Ison.Usage, UsageEpilog = Ison.Epilogue)]
	public class IsonOptions : GetOpt{
		[Parameters(Min = 1)]
		public List<string> Parameters = new List<string>();
	}
}
