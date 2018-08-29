using System;
using System.Collections.Generic;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.ChatBridge;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FuzzyString;
using NMaier.GetOptNet;

namespace dIRCordCS.Commands{
	// ReSharper disable once UnusedMember.Global
	// Loaded via reflection
	public class Ison : ICommand{
		static Ison(){
			ChatBridge.Bridge.RegisterCommand(nameof(Ison), new Ison());
		}

		private static readonly ILog Logger = LogManager.GetLogger<Ison>();

		public void HandleCommand(IrcListener listener, IrcChannel ircChannel, IList<string> args, PrivateMessageEventArgs e){
			try{
				IsonOptions opts = new IsonOptions();
				opts.Parse(args);
				DiscordChannel channel = listener.Config.ChannelMapObj[ircChannel];
				DiscordMember closestMatch;
				double closestMatchAmmount = 0;
				foreach(DiscordMember member in channel.Guild.Members){
					double matchAmount = member.Nickname.ToLower().NormalizedLevenshteinDistance("lil-g");
					if(matchAmount > closestMatchAmmount){
						closestMatch = member;
						closestMatchAmmount = matchAmount;
					}
				}

			}
			catch(GetOptException ex){
				ChatBridge.Bridge.Respond($"Sorry there was a problem processing the command: {ex.Message}", ircChannel);
				Logger.Error($"Problem processing command: \n{ex}");
			}
			catch(Exception ex){
				ChatBridge.Bridge.Respond($"Sorry there was a problem processing the command: {ex.Message}", ircChannel);
				Logger.Error($"Problem processing command: \n{ex}");
			}
		}

		public void HandleCommand(DiscordListener listener, IList<string> args, MessageCreateEventArgs e){
			IrcChannel channel = listener.Config.ChannelMapObj.Reverse[e.Channel];
		}
	}

	[GetOptOptions(OnUnknownArgument = UnknownArgumentsAction.Throw, UsageEpilog = "That's all, folks")]
	public class IsonOptions : GetOpt{
		[Parameters(Min = 1)]
		public List<string> Parameters = new List<string>();
	}
}
