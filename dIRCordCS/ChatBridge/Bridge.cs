using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.Commands;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FuzzyString;

namespace dIRCordCS.ChatBridge{
	public static class Bridge{
		public enum MessageType{ Message, Notice, PrivateMessage }
		private static readonly ILog Logger = LogManager.GetLogger(typeof(Bridge));
		private static readonly Commands.Commands Commands;
		static Bridge(){
			Commands = new Commands.Commands();
			IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
											   .SelectMany(s=>s.GetTypes())
											   .Where(p=>typeof(ICommand).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
			foreach(Type command in types){
				RuntimeHelpers.RunClassConstructor(command.TypeHandle);
				Logger.InfoFormat("Loaded command {0} as {1}", command.Name, command.FullName);
			}
		}

		public static async Task<bool> CommandHandler(IrcListener listener,
													  IrcChannel channel,
													  PrivateMessageEventArgs e){
			if(e.PrivateMessage.User.Hostmask == listener.IrcSelf.Hostmask){ return false; }

			string[] args = e.PrivateMessage.Message.Split(' ');
			if(args.Length < 2){ return false; }

			if(args[0].ToLower().StartsWith(listener.IrcSelf.Nick.ToLower())){
				string command;
				if(Commands.ContainsCommand(command = args[1])){
					ArraySegment<string> segment = new ArraySegment<string>(args, 2, args.Length - 2);
					try{ Commands[command].HandleCommand(listener, channel, segment, e); } catch(Exception ex){
						Logger.Error($"Problem processing command: \n{ex}");
						await Respond($"Sorry there was a problem processing the command: {ex.Message}", channel);
						return false;
					}

					return true;
				}
			}

			return false;
		}

		public static async Task<bool> CommandHandler(DiscordListener listener, DiscordMember member, MessageCreateEventArgs e){
			if(e.Author == e.Client.CurrentUser){ return false; }

			string[] args = e.Message.Content.Split(' ');
			if(args[0].StartsWith(e.Client.CurrentUser.Mention)      ||
			   args[0].StartsWith(e.Guild.CurrentMember.DisplayName) ||
			   args[0].StartsWith(e.Client.CurrentUser.Username)){
				string command;
				if(Commands.ContainsCommand(command = args[1].ToLower())){
					ArraySegment<string> segment = new ArraySegment<string>(args, 2, args.Length - 2);
					try{ Commands[command].HandleCommand(listener, member, segment, e); } catch(Exception ex){
						Logger.Error($"Problem processing command: \n{ex}");
						await Respond($"Sorry there was a problem processing the command: {ex.Message}", e.Channel);
						return false;
					}

					return true;
				}
			}

			return false;
		}

		public static void RegisterCommand(string commandName, ICommand command){Commands[commandName.ToLower()] = command;}

		public static async Task Respond(string message,
										 IrcChannel channel,
										 IrcUser user = null,
										 IrcClient client = null,
										 MessageType messageType = MessageType.Message){
			if(user != null){ message = $"{user.Nick}: {message}"; }

			if((messageType == MessageType.Message) ||
			   (client      == null)                ||
			   (user        == null)){ await Task.Run(()=>channel.SendMessage(message.SanitizeForIRC())); } else if(messageType == MessageType.Notice){
				await Task.Run(()=>client.SendNotice(message.SanitizeForIRC(), user.Nick));
			} else if(messageType == MessageType.PrivateMessage){ await Task.Run(()=>client.SendMessage(message.SanitizeForIRC(), user.Nick)); } else{
				throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
			}
		}

		public static async Task<DiscordMessage> Respond(string message,
														 DiscordChannel channel,
														 DiscordMember user = null,
														 DiscordClient client = null,
														 MessageType messageType = MessageType.Message){
			if(user != null){ message = $"{user.DisplayName}: {message}"; }

			if((messageType == MessageType.Message) ||
			   (client      == null)                ||
			   (user        == null)){ return await channel.SendMessageAsync(message); }

			if((messageType == MessageType.Notice) ||
			   (messageType == MessageType.PrivateMessage)){ return await user.SendMessageAsync(message); }

			throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
		}

		public static async Task SendMessage(string message,
											 IrcChannel channel,
											 IrcUser user,
											 IrcListener listener,
											 byte configId){
			if(user.Hostmask == listener.Config.IrcClient.User.Hostmask){ return; }

			DiscordChannel targetChannel = GetChannel(listener, channel);
			if(targetChannel == null){
				return; // The only way this could happen is if the bot gets SAJoined
			}

			string formattedUser = user.FormatName(configId);
			message = await IrcUtils.ConvertFormatting(message, targetChannel);
			await targetChannel.SendMessageAsync($"<{formattedUser}> {message}");
		}

		public static async Task SendMessage(string message,
											 DiscordChannel channel,
											 DiscordMember user,
											 DiscordListener listener,
											 byte configId){
			if(user == channel.Guild.CurrentMember){ return; }

			IrcChannel targetChannel = GetChannel(listener, channel);
			if(targetChannel == null){ return; }

			string formattedUser = user.FormatName(configId);
			message = await DiscordUtils.ConvertFormatting(message);
			message = message.SanitizeForIRC();
			targetChannel.SendMessage($"<{formattedUser}> {message}");
		}

		public static async Task<(IrcUser, double)> SearchForIRCUser(string search, IrcChannel channel, IrcListener listener){
			IrcUser closestMatchNick = null, closestMatchUsername = null, closestMatch = null;
			double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5, closestMatchScore = 5;
			foreach(IrcUser user in channel.Users){
				double matchAmount = user.Nick.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchNickAmount){
					closestMatchNick = user;
					closestMatchNickAmount = matchAmount;
				}

				if(user.User == null){ continue; }

				matchAmount = user.User.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchUsernameAmount){
					closestMatchUsername = user;
					closestMatchUsernameAmount = matchAmount;
				}
			}

			if((closestMatchNick     != null) ||
			   (closestMatchUsername != null)){
				if(closestMatchNickAmount < closestMatchUsernameAmount){
					closestMatch = closestMatchNick;
					closestMatchScore = closestMatchNickAmount;
				} else{
					closestMatch = closestMatchUsername;
					closestMatchScore = closestMatchUsernameAmount;
				}
			}

			return (closestMatch, closestMatchScore);
		}

		public static async Task<(DiscordMember, double)> SearchForDiscordUser(string search, DiscordChannel channel){
			DiscordMember closestMatchNick = null, closestMatchUsername = null, closestMatch = null;
			double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5, closestMatchScore = 5;
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

				if(member.Nickname == null){ continue; }

				matchAmount = member.Nickname.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchNickAmount){
					closestMatchNick = member;
					closestMatchNickAmount = matchAmount;
				}
			}

			if((closestMatchNick     != null) ||
			   (closestMatchUsername != null)){
				if(closestMatchNickAmount < closestMatchUsernameAmount){
					closestMatch = closestMatchNick;
					closestMatchScore = closestMatchNickAmount;
				} else{
					closestMatch = closestMatchUsername;
					closestMatchScore = closestMatchUsernameAmount;
				}
			}

			return (closestMatch, closestMatchScore);
		}

		public static DiscordChannel GetChannel(IrcListener listener, IrcChannel channel)=>
			listener.Config.ChannelMapObj.TryGetValue(channel, out DiscordChannel discordChannel) ? discordChannel : null;

		public static IrcChannel GetChannel(DiscordListener listener, DiscordChannel channel)=>
			listener.Config.ChannelMapObj.Reverse.TryGetValue(channel, out IrcChannel ircChannel) ? ircChannel : null;

		public static async void FillMap(byte configId){
			lock(Logger){
				if(!Program.Config[configId].IRCReady ||
				   !Program.Config[configId].DiscordReady){ return; }
			}

			BiDictionary<string, ulong> channelMapping = Program.Config[configId].ChannelMapping;
			ChannelCollection channels = Program.Config[configId].IrcClient.Channels;
			Program.Config[configId].ChannelMapObj =
				Program.Config[configId].ChannelMapObj ?? new BiDictionary<IrcChannel, DiscordChannel>();
			foreach(string channel in channelMapping.Keys){
				if(channels.Contains(channel)){
					DiscordChannel discordChannel =
						await Program.Config[configId].DiscordSocketClient.GetChannelAsync(channelMapping[channel]);
					IrcChannel ircChannel = channels[channel];
					Program.Config[configId].ChannelMapObj[ircChannel] = discordChannel;
				}
			}
		}
	}
}
