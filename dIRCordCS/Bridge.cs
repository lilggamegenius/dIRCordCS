/**
 	* Created by ggonz on 4/4/2017.
 	*/

using System;
using System.Text;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Abstractions;
using Common.Logging;
using IrcDotNet;
using IrcDotNet.Collections;

namespace dIRCordCS{
	public static class Bridge{
		private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
		private static readonly string escapePrefix = "@!";

		private static DiscordChannel getDiscordChannel(byte configID, IrcChannel channel){
			return Program.Config[configID].channelMapObj[channel];
		}

		private static IrcChannel getIRCChannel(byte configID, DiscordMessage @event){
			return Program.Config[configID].channelMapObj[@event.Channel];
		}

		public static void handleCommand(string command, string[] args, object eventObj, object channelObj, byte configID, bool IRC){
			// if IRC is true, then command called from IRC
			switch(command.ToLower()){
			case "help":{
				if(args.Length > 0){
					switch(args[0].ToLower()){
					case "help":{
						sendMessage(eventObj, ">_>", IRC);
					}
						break;
					case "whois":{
						sendMessage(eventObj,
						            "This command tells you info about a user from the other side of the bridge. The only argument is the name of the user",
						            IRC);
					}
						break;
					case "ison":{
						sendMessage(eventObj,
						            "This command tells you if a user on the other side of the bridge is online. The only argument is the name of the user",
						            IRC);
					}
						break;
					}
				}
				else{
					sendMessage(eventObj,
					            "Run of the mill help command, for help with a command, just use the command name as the argument. List of commands [whois, ison]",
					            IRC);
				}
			}
				break;
			case "whois":{
				if(args.Length > 0){
					var name = argJoiner(args);
					if(IRC){
						IrcMessageEventArgs @event = (IrcMessageEventArgs)eventObj;
						IrcChannel channel = (IrcChannel)channelObj;
						var members = getDiscordChannel(configID, channel).Guild.Members;
						DiscordMember member = null;
						foreach(var temp in members){
							if(!name.EqualsAnyIgnoreCase(temp.Nickname, temp.Username, Convert.ToString(temp.Id)))
								continue;
							member = temp;
							break;
						}

						if(member != null){
							string nickname, username, ID, status, avatar, game, joinDate, registerDate, roles, permissions;
							GameStreamType streaming;
							nickname = member.DisplayName;
							username = member.Username;
							ID = Convert.ToString(member.Id);
							status = member.Presence.Status.ToString();
							avatar = member.AvatarUrl;
							TransportGame gameObj = member.Presence.Game;
							streaming = gameObj.StreamType;
							game = streaming == GameStreamType.NoStream ? gameObj.Name : gameObj.Url;
							joinDate = member.JoinedAt.ToString("EEE, d MMM yyyy h:mm:ss a Z");
							registerDate = member.CreationTimestamp.ToString("EEE, d MMM yyyy h:mm:ss a Z");
							StringBuilder rolesBuilder = new StringBuilder();
							bool first = true;
							Permissions permission = 0;
							foreach(DiscordRole role in member.Roles){
								permission |= role.Permissions;
								if(!first){
									rolesBuilder.Append(", ");
								}
								else{
									first = false;
								}

								rolesBuilder.Append(role.Name);
							}

							roles = rolesBuilder.ToString();
							permissions = permission.ToPermissionString();
							channel.Client.LocalUser.SendMessage(@event.Targets, $"{name} is {nickname}!{username}@{ID} Status:{status} Currently {streaming} {game}");
							channel.Client.LocalUser.SendMessage(@event.Targets, $"Registered: {registerDate} Joined: {joinDate} Avatar: {avatar}");
							channel.Client.LocalUser.SendNotice(@event.Targets, $"Roles: [{roles}] Permissions: [{permissions}]");
						}
						else{
							channel.Client.LocalUser.SendMessage(@event.Targets, $"No one with the name \"{name}\" was found");
						}
					}
					else{
						DiscordMessage @event = (DiscordMessage)eventObj;
						string nick, username, hostname;
						string hostmask, realName, awayMsg, server;
						bool away;
						foreach(IrcChannelUser user in getIRCChannel(configID, @event).Users){
							nick = user.User.NickName;
							username = user.User.UserName;
							hostname = user.User.HostName;
							if(!(name.EqualsAnyIgnoreCase(nick, username, hostname) ||
							     name.StartsWithAny(nick, username, hostname)))
								continue;
							realName = user.User.RealName;
							hostmask = $"{nick}!{username}@{hostname}";
							away = user.User.IsAway;
							awayMsg = user.User.AwayMessage;
							server = user.User.ServerName;
							StringBuilder channelsBuilder = new StringBuilder();
							bool first = true;
							foreach(IrcChannelUser channel in user.User.GetChannelUsers()){
								char userLevel = getUserLevel(channel.Modes);
								if(!first){
									channelsBuilder.Append(", ");
								}

								if(userLevel == '\0'){
									channelsBuilder.Append(channel.Channel.Name);
								}
								else{
									channelsBuilder.Append(userLevel.getSymbol()).Append(channel.Channel.Name);
								}

								first = false;
							}

							sendMessage(eventObj,
							            "```\n" +
							            $"{nick} is {hostmask}\n" +
							            $"{nick}'s real name: {realName}\n" +
							            $"{(away ? nick + " Is away: " + awayMsg : "")}\n" +
							            $"{nick}'s channels: {channelsBuilder.toString()}\n" +
							            $"{nick}'s server: {server}\n" +
							            "```",
							            IRC);
							break;
						}
					}
				}
				else{
					sendMessage(eventObj, "Missing argument", IRC);
				}
			}
				break;
			case "ison":{
				if(args.Length > 0){
					string name = argJoiner(args, 0);
					if(IRC){
						IrcMessageEventArgs @event = (IrcMessageEventArgs)eventObj;
						var members = getDiscordChannel(configID, @event).Guild.Members;
						DiscordMember member = null;
						foreach(var temp in members){
							if(!name.EqualsAnyIgnoreCase(temp.Nickname, temp.Username, Convert.ToString(temp.Id)))
								continue;
							member = temp;
							break;
						}

						if(member != null){
							string nickname;
							nickname = member.DisplayName;
							bool online = member.Presence.Status != UserStatus.Offline;
							@event.respond(string.Format("{0} is {1}",
							                             nickname,
							                             online ? "online" : "offline"));
						}
						else{
							@event.respond(string.Format("No one with the name \"{0}\" was found", name));
						}
					}
					else{
						DiscordMessage @event = (DiscordMessage)eventObj;
						string nick, username, hostname;
						User user = null;
						foreach(User curUser in getIRCChannel(configID, @event).getUsers()){
							nick = curUser.getNick();
							username = curUser.getLogin();
							hostname = curUser.getHostname();
							if(name.EqualsAnyIgnoreCase(nick, username, hostname) ||
							   name.StartsWithAny(nick, username, hostname)){
								user = curUser;
								break;
							}
						}

						if(user != null){
							sendMessage(eventObj, user.getNick() + " Is online", IRC);
						}
						else{
							sendMessage(eventObj, name + " Is not online", IRC);
						}
					}
				}
			}
				break;
			case "topic":{
				if(IRC){
					IrcMessageEventArgs @event = (IrcMessageEventArgs)eventObj;
					sendMessage(eventObj,
					            string.Format("Topic: \"{0}\" set by {1} at {2}",
					                          @event.getChannel().getTopic(),
					                          @event.getChannel().getTopicSetter(),
					                          @event.getChannel().getTopicTimestamp()),
					            IRC);
				}
				else{
					DiscordMessage @event = (DiscordMessage)eventObj;
					sendMessage(eventObj, string.Format("Topic: {0}", @event.Channel.Topic), IRC);
				}
			}
				break;
			case "rehash":{
				if(IRC){
					//"#bridge-test": "#SSRG-Test"
					IrcMessageEventArgs @event = (IrcMessageEventArgs)eventObj;
					if(@event.getChannel().getUserLevels(@event.getUser()) != null){
						Program.rehash();
					}
				}
				else{
					DiscordMessage @event = (DiscordMessage)eventObj;
					if(((DiscordMember)@event.Author).PermissionsIn(@event.Channel).HasPermission(Permissions.Administrator)){
						Program.rehash();
					}
				}
			}
				break;
			}
		}

		public static void handleCommand(string[] message, object @event, byte configID, bool IRC){
			string[] args = {};
			string command = message[1];
			if(message.Length > 2){
				args = new string[message.Length - 2];
				try{
					Array.Copy(message, 2, args, 0, message.Length - 2);
				}
				catch(Exception e){
					Logger.Error($"array copy error {e}");
				}
			}

			handleCommand(command, args, @event, configID, IRC);
		}

		private static void sendMessage(object eventObj, string message, bool IRC){
			sendMessage(eventObj, message, IRC, true);
		}

		private static void sendMessage(object eventObj, string message, bool IRC, bool highlight){
			if(IRC){
				IrcMessageEventArgs @event = (IrcMessageEventArgs)eventObj;
				if(highlight){
					@event.respond(message);
				}
				else{
					@event.respondWith(message);
				}
			}
			else{
				DiscordMessage @event = (DiscordMessage)eventObj;
				if(highlight){
					@event.Channel.SendMessageAsync(string.Format("{0}: {1}", @event.Author.Mention, message)).Start();
				}
				else{
					@event.Channel.SendMessageAsync(message).Start();
				}
			}
		}

		private static string argJoiner(string[] args, int argToStartFrom = 0){
			if(args.Length - 1 == argToStartFrom){
				return args[argToStartFrom];
			}

			var strToReturn = new StringBuilder();
			for(var length = args.Length; length > argToStartFrom; argToStartFrom++){
				strToReturn.Append(args[argToStartFrom]).Append(" ");
			}

			Logger.Debug("Argument joined to: " + strToReturn);
			return strToReturn.Length == 0
				       ? strToReturn.ToString()
				       : strToReturn.ToString().Substring(0, strToReturn.Length - 1);
		}

		public static char getUserLevel(ReadOnlySet<char> levels){
			if(levels.Contains('q'))
				return 'q';
			if(levels.Contains('a'))
				return 'a';
			if(levels.Contains('o'))
				return 'o';
			if(levels.Contains('h'))
				return 'h';
			if(levels.Contains('v'))
				return 'v';
			return '\0';
		}

		public static string formatString(DiscordChannel channel, string strToFormat){
			const char underline = '\u001F';
			const char italics = '\u001D';
			const char bold = '\u0002';
			const char color = '\u0003';
			const char reverse = '\u0016';
			int reverseCount = strToFormat.Split(reverse).Length - 1;
			int underlineCount = strToFormat.Split(underline).Length - 1;
			int italicsCount = strToFormat.Split(italics).Length - 1;
			int boldCount = strToFormat.Split(bold).Length - 1;
			if(reverseCount != 0){
				strToFormat = strToFormat.replace(reverse, '`');
				if(reverseCount % 2 != 0){
					strToFormat += '`';
				}
			}

			if(underlineCount != 0){
				strToFormat = strToFormat.replace(underline + "", "__");
				if(underlineCount % 2 != 0){
					strToFormat += "__";
				}
			}

			if(italicsCount != 0){
				strToFormat = strToFormat.replace(italics, '_');
				if(italicsCount % 2 != 0){
					strToFormat += "_";
				}
			}

			if(boldCount != 0){
				strToFormat = strToFormat.replace(bold + "", "**");
				if(boldCount % 2 != 0){
					strToFormat += "**";
				}
			}

			if(strToFormat.contains("@")){
				strToFormat = strToFormat.replace("@everyone", "`@everyone`");
				if(strToFormat.contains(escapePrefix)){
					var message = strToFormat.splitMessage(0, false);
					for(var i = 0; i < message.Length; i++){
						if(!message[i].startsWith(escapePrefix))
							continue;
						message[i] = message[i].substring(escapePrefix.Length);
						switch(message[i]){
						case "last":
							message[i] = Program.LastUserToSpeak[channel].Mention;
							break;
						}
					}

					strToFormat = argJoiner(message);
				}

				string strLower = strToFormat.ToLower();
				bool usesNick;
				foreach(DiscordMember member in channel.Guild.Members){
					string memberName = member.Nickname;
					string userName = member.Username;
					while(strLower.contains("@" + memberName) ||
					      strLower.contains("@" + userName)){
						usesNick = true;
						int index = strLower.indexOf(memberName);
						if(index == -1){
							index = strLower.indexOf(userName);
							usesNick = false;
						}

						strToFormat = strToFormat.substring(0, index - 1) +
						              member.Mention +
						              strToFormat.substring(index + (usesNick ? memberName : userName).Length);
						strLower = strToFormat.ToLower();
					}
				}
			}

			if(strToFormat.contains(color + "")){
				strToFormat = strToFormat.replaceAll(color + "[0-9]{2}", "");
			}

			return strToFormat;
		}

		static string formatString(string message){
			char underline = '\u001F';
			char italics = '\u001D';
			char bold = '\u0002';
			char reverse = '\u0016';
			message = message.Replace("{", "{{").Replace("}", "}}");
			// find links
			string[] parts = message.Split(' ');
			int i = 0;
			bool inBlockComment = false;
			int blockCount = message.countMatches("```");
			for(int partsLength = parts.Length; i < partsLength; i++){
				string item = parts[i];
				if(item.CheckURLValid()){
					message = message.replace(item, "{" + i + "}");
					continue;
				}

				if(item.startsWith("```") &&
				   blockCount > 1){
					inBlockComment = true;
					message = message.replace(item, "{" + i + "}");
					blockCount--;
					if(item.endsWith("```")){
						inBlockComment = false;
						blockCount--;
					}
				}

				if(inBlockComment){
					if(item.endsWith("```")){
						inBlockComment = false;
					}

					message = message.replace(item, "{" + i + "}");
					blockCount--;
				}
			}

			int inlineCodeCount = message.countMatches("`");
			if(inlineCodeCount > 1){
				if(inlineCodeCount % 2 != 0){
					for(int count = 0; count < inlineCodeCount; count++){
						message = message.replace('`', reverse);
					}
				}
				else{
					message = message.replace('`', reverse);
				}
			}

			int underlineCount = message.countMatches("__");
			if(underlineCount > 1){
				if(underlineCount % 2 != 0){
					for(int count = 0; count < underlineCount; count++){
						message = message.replace("__", underline + "");
					}
				}
				else{
					message = message.replace("__", underline + "");
				}
			}

			int boldCount = message.countMatches("**");
			if(boldCount > 1){
				if(boldCount % 2 != 0){
					for(int count = 0; count < boldCount; count++){
						message = message.replace("**", bold + "");
					}
				}
				else{
					message = message.replace("**", bold + "");
				}
			}

			int italicsCount = message.countMatches("_");
			if(italicsCount > 1){
				if(italicsCount % 2 != 0){
					for(int count = 0; count < italicsCount; count++){
						message = message.replace('_', italics);
					}
				}
				else{
					message = message.replace('_', italics);
				}
			}

			italicsCount = message.countMatches("*");
			if(italicsCount > 1){
				if(italicsCount % 2 != 0){
					for(int count = 0; count < italicsCount; count++){
						message = message.replace('*', italics);
					}
				}
				else{
					message = message.replace('*', italics);
				}
			}

			message = message
			          .replace('\u0007', '␇')
			          .replace('\n', '␤')
			          .replace('\r', '␍');
			return string.Format(message, parts);
		}
	}
}
