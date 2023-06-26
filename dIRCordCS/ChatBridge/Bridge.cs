using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.Commands;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using FuzzyString;
using NLog;

namespace dIRCordCS.ChatBridge;

public static class Bridge{
	public enum MessageType{ Message, Notice, PrivateMessage }
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	private static readonly Commands.Commands Commands;
	static Bridge(){
		Commands = new Commands.Commands();
		// Loops through all classes and runs their static constructors
		IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
										   .SelectMany(s=>s.GetTypes())
										   .Where(p=>typeof(ICommand).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
		foreach(Type command in types){
			RuntimeHelpers.RunClassConstructor(command.TypeHandle);
			Logger.Info("Loaded command {0} as {1}", command.Name, command.FullName);
		}
	}

	public static async Task<bool> CommandHandler(
		IrcListener listener,
		IrcChannel channel,
		PrivateMessageEventArgs e
	){
		if(e.PrivateMessage.User.Hostmask == listener.IrcSelf.Hostmask){
			return false;
		}

		string[] args = e.PrivateMessage.Message.Split(' ');
		if(args.Length < 2){
			return false;
		}

		if(!args[0].ToLower().StartsWith(listener.IrcSelf.Nick.ToLower())) return false;
		string command;
		if(!Commands.ContainsCommand(command = args[1])) return false;
		ArraySegment<string> segment = new(args, 2, args.Length - 2);
		try{
			await Commands[command].HandleCommand(listener, channel, segment, e);
		} catch(ResetException){
			throw;
		} catch(Exception ex){
			Logger.Error($"Problem processing command: \n{ex}");
			await Respond($"Sorry there was a problem processing the command: {ex.Message}", channel);
			return false;
		}

		return true;
	}

	public static async Task<bool> CommandHandler(DiscordListener listener, DiscordClient client, DiscordMember member, MessageCreateEventArgs e){
		DiscordMember currentMember = e.Guild.CurrentMember;
		if((member == null) ||
		   (member == currentMember)){
			return false;
		}

		string[] args = e.Message.Content.Split(' ');
		if(!args[0].StartsWith(currentMember.Mention)     &&
		   !args[0].StartsWith(currentMember.DisplayName) &&
		   !args[0].StartsWith(currentMember.Username)){
			return false;
		}

		string command;
		if(!Commands.ContainsCommand(command = args[1].ToLower())) return false;
		ArraySegment<string> segment = new(args, 2, args.Length - 2);
		try{
			await Commands[command].HandleCommand(listener, member, segment, e);
		} catch(Exception ex){
			Logger.Error($"Problem processing command: \n{ex}");
			await Respond($"Sorry there was a problem processing the command: {ex.Message}", e.Channel);
		}

		return true;
	}

	public static void RegisterCommand(string commandName, ICommand command){Commands[commandName.ToLower()] = command;}

	public static async Task Respond(
		string message,
		IrcChannel channel,
		IrcUser user = null,
		IrcClient client = null,
		MessageType messageType = MessageType.Message
	){
		if(user != null){
			message = $"{user.Nick}: {message}";
		}

		if((messageType == MessageType.Message) ||
		   (client      == null)                ||
		   (user        == null)){
			await Task.Run(()=>channel.SendMessage(message.SanitizeForIRC()));
			return;
		}

		switch(messageType){
			case MessageType.Notice:
				await client.SendNoticeAsync(message.SanitizeForIRC(), user.Nick);
				break;
			case MessageType.PrivateMessage:
				await client.SendMessageAsync(message.SanitizeForIRC(), user.Nick);
				break;
			case MessageType.Message: // Not Possible due to above "if"
			default: throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
		}
	}

	public static async Task<DiscordMessage> Respond(
		string message,
		DiscordChannel channel,
		DiscordMember user = null,
		DiscordClient client = null,
		MessageType messageType = MessageType.Message
	){
		if(user != null){
			message = $"{user.DisplayName}: {message}";
		}

		if((messageType == MessageType.Message) ||
		   (client      == null)                ||
		   (user        == null)){
			return await channel.SendMessageAsync(message);
		}

		if(messageType is MessageType.Notice or MessageType.PrivateMessage){
			return await user.SendMessageAsync(message);
		}

		throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
	}

	public static async Task SendMessage(
		string message,
		IrcChannel channel,
		IrcUser user,
		IrcListener listener,
		byte configId
	){
		if(user.Hostmask == listener.Config.IrcClient.User.Hostmask){
			return;
		}

		DiscordChannel targetChannel = GetChannel(listener, channel);
		if(targetChannel == null){
			return; // The only way this could happen is if the bot gets SAJoined or the bot didn't get enough time to set up all channels
		}

		string formattedUser = user.FormatName(configId);
		message = await IrcUtils.ConvertFormatting(message, targetChannel);
		await targetChannel.SendMessageAsync($"<{formattedUser}> {message}");
	}

	public static async Task SendMessage(
		DiscordMessage discordMessage,
		DiscordChannel channel,
		DiscordMember user,
		DiscordListener listener,
		byte configId
	){
		if(user == channel.Guild.CurrentMember){
			return;
		}

		IrcChannel targetChannel = GetChannel(listener, channel);
		if(targetChannel == null){
			return;
		}

		string formattedUser = user.FormatName(configId);
		string message = await DiscordUtils.ConvertFormatting(discordMessage, configId);
		message = message.SanitizeForIRC();
		await targetChannel.SendMessageAsync($"<{formattedUser}> {message}");
	}

	public static async Task<(IrcUser, double)> SearchForIRCUser(string search, IrcChannel channel, IrcListener listener){
		IrcUser closestMatchNick = null, closestMatchUsername = null;
		double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5;
		await Task.Run(()=>{
			foreach(IrcUser user in channel.Users){
				double matchAmount = user.Nick.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchNickAmount){
					closestMatchNick = user;
					closestMatchNickAmount = matchAmount;
				}

				if(user.User == null){
					continue;
				}

				matchAmount = user.User.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount >= closestMatchUsernameAmount) continue;
				closestMatchUsername = user;
				closestMatchUsernameAmount = matchAmount;
			}
		});
		if((closestMatchNick == null) && (closestMatchUsername == null)){
			return (null, 0);
		}

		return closestMatchNickAmount < closestMatchUsernameAmount ? (closestMatchNick, closestMatchNickAmount) : (closestMatchUsername, closestMatchUsernameAmount);
	}

	public static async Task<(DiscordMember, double)> SearchForDiscordUser(string search, DiscordChannel channel){
		DiscordMember closestMatchNick = null, closestMatchUsername = null, closestMatch = null;
		double closestMatchNickAmount = 5, closestMatchUsernameAmount = 5, closestMatchScore = 5;
		Logger.Debug($"Getting Member list for #{channel.Name}");
		foreach(DiscordMember member in await channel.Guild.GetAllMembersAsync()){
			await Task.Run(()=>{
				Logger.Debug($"Discord User Search ({search}): {member.GetHostMask()}");
				if(search == member.Id.ToString()){
					closestMatch = member;
					closestMatchNick = closestMatchUsername = null;
					closestMatchNickAmount = closestMatchUsernameAmount = -1;
					return; //break;
				}

				double matchAmount = member.Username.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount < closestMatchUsernameAmount){
					closestMatchUsername = member;
					closestMatchUsernameAmount = matchAmount;
				}

				if(member.Nickname == null){
					return; //continue;
				}

				matchAmount = member.Nickname.ToLower().NormalizedLevenshteinDistance(search);
				if(matchAmount >= closestMatchNickAmount) return;
				closestMatchNick = member;
				closestMatchNickAmount = matchAmount;
			});
		}

		if((closestMatchNick     == null) &&
		   (closestMatchUsername == null)){
			return (closestMatch, closestMatchScore);
		}

		if(closestMatchNickAmount < closestMatchUsernameAmount){
			closestMatch = closestMatchNick;
			closestMatchScore = closestMatchNickAmount;
		} else{
			closestMatch = closestMatchUsername;
			closestMatchScore = closestMatchUsernameAmount;
		}

		return (closestMatch, closestMatchScore);
	}

	public static DiscordChannel GetChannel(IrcListener listener, IrcChannel channel)=>listener.Config.ChannelMapObj.TryGetValue(channel, out DiscordChannel discordChannel) ? discordChannel : null;

	public static IrcChannel GetChannel(DiscordListener listener, DiscordChannel channel)=>listener.Config.ChannelMapObj.Reverse.TryGetValue(channel, out IrcChannel ircChannel) ? ircChannel : null;

	public static async void FillMap(byte configId){
		lock(Logger){
			if(!Program.Config.Servers[configId].IRCReady ||
			   !Program.Config.Servers[configId].DiscordReady){
				return;
			}
		}

		BiDictionary<string, ulong> channelMapping = Program.Config.Servers[configId].ChannelMapping;
		ChannelCollection channels = Program.Config.Servers[configId].IrcClient.Channels;
		Program.Config.Servers[configId].ChannelMapObj ??= new BiDictionary<IrcChannel, DiscordChannel>();
		foreach(string channel in channelMapping.Keys){
			if(!channels.Contains(channel)){
				continue;
			}

			DiscordChannel discordChannel;
			ulong channelId = channelMapping[channel];
			try{
				discordChannel = await Program.Config.Servers[configId].DiscordClient.GetChannelAsync(channelId);
			} catch(NotFoundException e){
				Logger.Error(e, $"Invalid Discord Channel in config: [{channelId}] Mapped to [{channel}] in server [{Program.Config.Servers[configId].Server}]");
				continue;
			} catch(UnauthorizedException e){
				Logger.Fatal(e, $"Possibly some sort of permission error? Channel ID [{channelId}] Mapped to [{channel}] in server [{Program.Config.Servers[configId].Server}]");
				throw;
			}

			IrcChannel ircChannel = channels[channel];
			Program.Config.Servers[configId].ChannelMapObj[ircChannel] = discordChannel;
		}
	}
}
