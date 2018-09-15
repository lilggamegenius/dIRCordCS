using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.Commands;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FuzzyString;

namespace dIRCordCS.ChatBridge{
	public static class Bridge{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(Bridge));
		private static readonly Commands.Commands commands;
		static Bridge(){
			commands = new Commands.Commands();
			var types = AppDomain.CurrentDomain.GetAssemblies()
			                     .SelectMany(s=>s.GetTypes())
			                     .Where(p=>typeof(ICommand).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
			foreach(Type Command in types){
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(Command.TypeHandle);
				Logger.InfoFormat("Loaded command {0} as {1}", Command.Name, Command.FullName);
			}
		}

		public static bool CommandHandler(IrcListener listener, IrcChannel channel, PrivateMessageEventArgs e){
			if(e.PrivateMessage.User.Hostmask == listener.ircSelf.Hostmask) return false;
			string[] args = e.PrivateMessage.Message.Split(' ');
			if(args.Length < 2)
				return false;
			if(args[0].ToLower().StartsWith(listener.ircSelf.Nick.ToLower())){
				string command;
				if(commands.ContainsCommand(command = args[1])){
					var segment = new ArraySegment<string>(args, 2, args.Length - 2);
					commands[command].HandleCommand(listener, channel, segment, e);
					return true;
				}
			}

			return false;
		}

		public static bool CommandHandler(DiscordListener listener, MessageCreateEventArgs e){
			if(e.Author == e.Client.CurrentUser) return false;
			string[] args = e.Message.Content.Split(' ');
			if(args[0].StartsWith(e.Client.CurrentUser.Mention) ||
			   args[0].StartsWith(e.Guild.CurrentMember.DisplayName) ||
			   args[0].StartsWith(e.Client.CurrentUser.Username)){
				string command;
				if(commands.ContainsCommand(command = args[1].ToLower())){
					var segment = new ArraySegment<string>(args, 2, args.Length - 2);
					commands[command].HandleCommand(listener, segment, e);
					return true;
				}
			}

			return false;
		}

		public static void RegisterCommand(string commandName, ICommand command){
			commands[commandName.ToLower()] = command;
		}

		public static async Task Respond(string message, IrcChannel channel, IrcUser user = null){
			if(user != null){
				message = $"{user.Nick}: {message}";
			}

			channel.SendMessage(message.SanitizeForIRC());
		}

		public static async Task<DiscordMessage> Respond(string message, DiscordChannel channel, DiscordMember user = null){
			if(user != null){
				message = $"{user.Nickname}: {message}";
			}
			return await channel.SendMessageAsync(message);
		}

		public static IrcUser SearchForIRCUser(string search, IrcChannel channel){
			IrcUser closestMatchNick = null, closestMatchUsername = null, closestMatch = null;
			double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5;
			foreach(IrcUser user in channel.Users){
				/*if(search == user.Id.ToString()){
					closestMatch = user;
					closestMatchNick = closestMatchUsername = null;
					break;
				}*/
				double matchAmount = user.User.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchUsernameAmount){
					closestMatchUsername = user;
					closestMatchUsernameAmount = matchAmount;
				}
				matchAmount = user.Nick.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchNickAmount){
					closestMatchNick = user;
					closestMatchNickAmount = matchAmount;
				}
			}

			if(closestMatchNick != null || closestMatchUsername != null){
				if(closestMatchNickAmount < closestMatchUsernameAmount){
					closestMatch = closestMatchNick;
				}
				else{
					closestMatch = closestMatchUsername;
				}
			}

			return closestMatch;
		}

		public static async Task<DiscordMember> SearchForDiscordUser(string search, DiscordChannel channel){
			DiscordMember closestMatchNick = null, closestMatchUsername = null, closestMatch = null;
			double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5;
			Logger.Debug($"Getting Member list for #{channel.Name}");
			foreach(DiscordMember member in await channel.Guild.GetAllMembersAsync()){
				Logger.Debug($"Discord User Search ({search}): {member.GetHostMask()}");
				if(search == member.Id.ToString()){
					closestMatch = member;
					closestMatchNick = closestMatchUsername = null;
					break;
				}

				double matchAmount = member.Username.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchUsernameAmount){
					closestMatchUsername = member;
					closestMatchUsernameAmount = matchAmount;
				}

				if(member.Nickname == null)
					continue;
				matchAmount = member.Nickname.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchNickAmount){
					closestMatchNick = member;
					closestMatchNickAmount = matchAmount;
				}
			}

			if(closestMatchNick != null || closestMatchUsername != null){
				if(closestMatchNickAmount < closestMatchUsernameAmount){
					closestMatch = closestMatchNick;
				}
				else{
					closestMatch = closestMatchUsername;
				}
			}

			return closestMatch;
		}

		public static DiscordChannel GetChannel(IrcListener listener, IrcChannel channel){
			DiscordChannel discordChannel;
			return listener.Config.ChannelMapObj.TryGetValue(channel, out discordChannel) ? discordChannel : null;
		}

		public static IrcChannel GetChannel(DiscordListener listener, DiscordChannel channel){
			IrcChannel ircChannel;
			return listener.Config.ChannelMapObj.Reverse.TryGetValue(channel, out ircChannel) ? ircChannel : null;
		}

		public static async void FillMap(byte configId){
			lock(Logger){
				if(!Program.Config[configId].IRCReady ||
				   !Program.Config[configId].DiscordReady){
					return;
				}
			}

			var channelMapping = Program.Config[configId].ChannelMapping;
			var channels = Program.Config[configId].IrcClient.Channels;
			Program.Config[configId].ChannelMapObj = Program.Config[configId].ChannelMapObj ?? new BiDictionary<IrcChannel, DiscordChannel>();
			foreach(string channel in channelMapping.Keys){
				if(channels.Contains(channel)){
					DiscordChannel discordChannel = await Program.Config[configId].DiscordSocketClient.GetChannelAsync(channelMapping[channel]);
					IrcChannel ircChannel = channels[channel];
					Program.Config[configId].ChannelMapObj[ircChannel] = discordChannel;
				}
			}
		}

		enum MessageType{
			Message, Notice, PrivateMessage
		}
	}
}
