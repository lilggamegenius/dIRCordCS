using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.google.common.collect;
using dIRCordCS.Utils;
using Discord;
using Discord.WebSocket;
using ikvm.extensions;
using NLog;
using org.pircbotx;
using org.pircbotx.hooks.events;

namespace dIRCordCS{
	/**
 	* Created by ggonz on 4/4/2017.
 	*/
		public static class Bridge {
		private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
		private static readonly string escapePrefix = "@!";

		private static ITextChannel getDiscordChannel(byte configID, MessageEvent @event) {
			return Program.config[configID].channelMapObj[@event.getChannel()];
		}
	
		private static Channel getIRCChannel(byte configID, IUserMessage @event) {
			return Program.config[configID].channelMapObj[(ITextChannel)@event.Channel];
		}
	
		private static void handleCommand(string command, string[] args, object eventObj, byte configID, bool IRC) { // if IRC is true, then command called from IRC
			switch (command.toLowerCase()) {
				case "help": {
					if (args.Length > 0) {
						switch (args[0].toLowerCase()) {
							case "help": {
								sendMessage(eventObj, ">_>", IRC);
							}
							break;
							case "whois": {
								sendMessage(eventObj, "This command tells you info about a user from the other side of the bridge. The only argument is the name of the user", IRC);
							}
							break;
							case "ison": {
								sendMessage(eventObj, "This command tells you if a user on the other side of the bridge is online. The only argument is the name of the user", IRC);
							}
							break;
						}
					} else {
						sendMessage(eventObj, "Run of the mill help command, for help with a command, just use the command name as the argument. List of commands [whois, ison]", IRC);
					}
				}
				break;
				case "whois": {
					if (args.Length > 0) {
						var name = argJoiner(args);
						if (IRC) {
							MessageEvent @event = (MessageEvent) eventObj;
							var members = getDiscordChannel(configID, @event).GetUsersAsync().Flatten().Result;
							IGuildUser member = null;
							foreach(var temp in members){
								if(!name.equalsAnyIgnoreCase(temp.Nickname, temp.Username, Convert.ToString(temp.Id))) continue;
								member = temp;
								break;
							}
							if (member != null) {
								string nickname, username, ID, status, avatar, game, joinDate, registerDate, roles, permissions;
								StreamType streaming;
								nickname = member.EffectiveName();
								username = member.Username;
								ID = Convert.ToString(member.Id);
								status = member.Status.ToString();
								avatar = member.GetAvatarUrl();
								Game? gameObj = member.Game;
								if (gameObj.HasValue) {
									streaming = gameObj.Value.StreamType;
									game = streaming == StreamType.NotStreaming ? gameObj.Value.Name : gameObj.Value.StreamUrl;
								} else {
									streaming = StreamType.NotStreaming;
									game = "nothing";
								}
								joinDate = member.JoinedAt?.ToString("EEE, d MMM yyyy h:mm:ss a Z");
								registerDate = member.CreatedAt.ToString("EEE, d MMM yyyy h:mm:ss a Z");
								StringBuilder rolesBuilder = new StringBuilder();
								bool first = true;
								foreach(IRole role in ((SocketGuildUser)member).Roles) {
									if (!first) {
										rolesBuilder.Append(", ");
									} else {
										first = false;
									}
									rolesBuilder.Append(role.Name);
								}
								roles = rolesBuilder.ToString();
								StringBuilder permissionsBuilder = new StringBuilder();
								first = true;
								foreach(GuildPermission permission in ((SocketGuildUser)member).GuildPermissions.ToList()) {
									if (!first) {
										permissionsBuilder.Append(", ");
									} else {
										first = false;
									}
									permissionsBuilder.Append(permission);
								}
								permissions = permissionsBuilder.toString();
								@event.respond($"{name} is {nickname}!{username}@{ID} Status:{status} Currently {streaming} {game}");
								@event.respond($"Registered: {registerDate} Joined: {joinDate} Avatar: {avatar}");
								@event.getUser().send().notice($"Roles: [{roles}] Permissions: [{permissions}]");
							} else {
								@event.respond($"No one with the name \"{name}\" was found");
							}
						} else {
							IUserMessage @event = (IUserMessage) eventObj;
							string nick, username, hostname;
							string hostmask, realName, awayMsg, server;
							bool away;
							foreach(User user in getIRCChannel(configID, @event).getUsers()) {
								nick = user.getNick();
								username = user.getLogin();
								hostname = user.getHostname();
								if (!(LilGUtil.equalsAnyIgnoreCase(name, nick, username, hostname) || LilGUtil.startsWithAny(name, nick, username, hostname)))
									continue;
								realName = user.getRealName();
								hostmask = user.getHostmask();
								away = user.isAway();
								awayMsg = user.getAwayMessage();
								server = user.getServer();
								StringBuilder channelsBuilder = new StringBuilder();
								bool first = true;
								foreach(Channel channel in user.getChannels()) {
									UserLevel userLevel = getUserLevel(channel.getUserLevels(user));
									if (!first) {
										channelsBuilder.Append(", ");
									}
									if (userLevel == null) {
										channelsBuilder.Append(channel.getName());
									} else {
										channelsBuilder.Append(userLevel.getSymbol()).Append(channel.getName());
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
											"```"
								, IRC);
								break;
							}
						}
					} else {
						sendMessage(eventObj, "Missing argument", IRC);
					}
				}
				break;
				case "ison": {
					if (args.Length > 0) {
						string name = argJoiner(args, 0);
						if (IRC) {
							MessageEvent @event = (MessageEvent) eventObj;
							var members = getDiscordChannel(configID, @event).GetUsersAsync().Flatten().Result;
							IGuildUser member = null;
							foreach(var temp in members){
								if(!name.equalsAnyIgnoreCase(temp.Nickname, temp.Username, Convert.ToString(temp.Id))) continue;
								member = temp;
								break;
							}
							if (member != null) {
								string nickname;
								nickname = member.EffectiveName();
								bool online = member.Status != UserStatus.Offline;
								@event.respond(string.Format("{0} is {1}",
										nickname,
										online ? "online" : "offline"));
							} else {
								@event.respond(string.Format("No one with the name \"{0}\" was found", name));
							}
						} else {
							IUserMessage @event = (IUserMessage) eventObj;
							string nick, username, hostname;
							User user = null;
							foreach(User curUser in getIRCChannel(configID, @event).getUsers()) {
								nick = curUser.getNick();
								username = curUser.getLogin();
								hostname = curUser.getHostname();
								if (LilGUtil.equalsAnyIgnoreCase(name, nick, username, hostname) || LilGUtil.startsWithAny(name, nick, username, hostname)) {
									user = curUser;
									break;
								}
							}
							if (user != null) {
								sendMessage(eventObj, user.getNick() + " Is online", IRC);
							} else {
								sendMessage(eventObj, name + " Is not online", IRC);
							}
						}
					}
				}
				break;
				case "topic": {
					if (IRC) {
						MessageEvent @event = (MessageEvent) eventObj;
						sendMessage(eventObj, string.Format("Topic: \"{0}\" set by {1} at {2}", @event.getChannel().getTopic(), @event.getChannel().getTopicSetter(), @event.getChannel().getTopicTimestamp()), IRC);
					} else {
						IUserMessage @event = (IUserMessage) eventObj;
						sendMessage(eventObj, string.Format("Topic: {0}", ((ITextChannel)@event.Channel).Topic), IRC);
					}
				}
				break;
				case "rehash": {
					if (IRC) {
						//"#bridge-test": "#SSRG-Test"
						MessageEvent @event = (MessageEvent) eventObj;
						if (@event.getChannel().getUserLevels(@event.getUser()) != null) {
							Program.rehash();
						}
					} else {
						IUserMessage @event = (IUserMessage) eventObj;
						if (@event.getMember().hasPermission(Permission.ADMINISTRATOR)) {
							Program.rehash();
						}
					}
				}
			}
		}
	
		static void handleCommand(string[] message, object @event, byte configID, bool IRC){
			string command; 
			string[] args = {};
			command = message[1];
			if (message.Length > 2) {
				args = new string[message.Length - 2];
				try {
					Array.Copy(message, 2, args, 0, message.Length - 2);
				} catch (Exception e) {
					LOGGER.Error($"array copy error {e}");
				}
			}
			handleCommand(command, args, @event, configID, IRC);
		}
	
		private static void sendMessage(object eventObj, string message, bool IRC) {
			sendMessage(eventObj, message, IRC, true);
		}
	
		private static void sendMessage(object eventObj, string message, bool IRC, bool highlight) {
			if (IRC) {
				MessageEvent @event = (MessageEvent) eventObj;
				if (highlight) {
					@event.respond(message);
				} else {
					@event.respondWith(message);
				}
			} else {
				IUserMessage @event = (IUserMessage) eventObj;
				if (highlight) {
					@event.getChannel().sendMessage(string.format("%s: %s", @event.getMember().getAsMention(), message)).complete();
				} else {
					@event.getChannel().sendMessage(message).complete();
				}
			}
		}

			private static string argJoiner(string[] args, int argToStartFrom = 0){
				if (args.Length - 1 == argToStartFrom) {
					return args[argToStartFrom];
				}
				var strToReturn = new StringBuilder();
				for (var length = args.Length; length > argToStartFrom; argToStartFrom++) {
					strToReturn.Append(args[argToStartFrom]).Append(" ");
				}
				LOGGER.Debug("Argument joined to: " + strToReturn);
				return strToReturn.Length == 0 ? strToReturn.ToString() : strToReturn.ToString().Substring(0, strToReturn.Length - 1);
			}
	
		private static UserLevel getUserLevel(ImmutableSortedSet/*<UserLevel>*/ levels) {
			if (levels.isEmpty()) {
				return null;
			}
			var ret = (from UserLevel level in levels select level.ordinal()).Concat(new[]{0}).Max();
			return ret == 0 ? null : UserLevel.values()[ret - 1];
		}
	
		static string formatString(SocketGuildChannel channel, string strToFormat) {
			const char underline = '\u001F';
			const char italics = '\u001D';
			const char bold = '\u0002';
			const char color = '\u0003';
			const char reverse = '\u0016';
			int reverseCount = strToFormat.Split(reverse).Length - 1;
			int underlineCount = strToFormat.Split(underline).Length - 1;
			int italicsCount = strToFormat.Split(italics).Length - 1;
			int boldCount = strToFormat.Split(bold).Length - 1;
			if (reverseCount != 0) {
				strToFormat = strToFormat.replace(reverse, '`');
				if (reverseCount % 2 != 0) {
					strToFormat += '`';
				}
			}
			if (underlineCount != 0) {
				strToFormat = strToFormat.replace(underline + "", "__");
				if (underlineCount % 2 != 0) {
					strToFormat += "__";
				}
			}
			if (italicsCount != 0) {
				strToFormat = strToFormat.replace(italics, '_');
				if (italicsCount % 2 != 0) {
					strToFormat += "_";
				}
			}
			if (boldCount != 0) {
				strToFormat = strToFormat.replace(bold + "", "**");
				if (boldCount % 2 != 0) {
					strToFormat += "**";
				}
			}
			if (strToFormat.contains("@")) {
				strToFormat = strToFormat.replace("@everyone", "`@everyone`");
				if (strToFormat.contains(escapePrefix)) {
					var message = LilGUtil.splitMessage(strToFormat, 0, false);
					for (var i = 0; i < message.Length; i++) {
						if (!message[i].startsWith(escapePrefix)) continue;
						message[i] = message[i].substring(escapePrefix.Length);
						switch (message[i]) {
							case "last":
								message[i] = Program.LastUserToSpeak[channel].Mention;
								break;
						}
					}
					strToFormat = argJoiner(message);
				}
				string strLower = strToFormat.toLowerCase();
				bool usesNick;
				foreach(SocketGuildUser member in channel.Users) {
					string memberName = member.Nickname;
					string userName = member.Username;
					while (strLower.contains("@" + memberName) || strLower.contains("@" + userName)) {
						usesNick = true;
						int index = strLower.indexOf(memberName);
						if (index == -1) {
							index = strLower.indexOf(userName);
							usesNick = false;
						}
						strToFormat = strToFormat.substring(0, index - 1) +
								member.Mention +
								strToFormat.substring(index + (usesNick ? memberName : userName).Length);
						strLower = strToFormat.toLowerCase();
					}
				}
			}
			if (strToFormat.contains(color + "")) {
				strToFormat = strToFormat.replaceAll(color + "[0-9]{2}", "");
			}
	
			return strToFormat;
		}
	
		static string formatString(string message) {
			char underline = '\u001F';
			char italics = '\u001D';
			char bold = '\u0002';
			char reverse = '\u0016';
			message = message.Replace("{", "{{").Replace("}", "}}");
			// find links
			string[] parts = message.Split(' ');
			int i = 0;
			bool inBlockComment = false;
			int blockCount = LilGUtil.countMatches(message, "```");
			for (int partsLength = parts.Length; i < partsLength; i++) {
				string item = parts[i];
				if(item.CheckURLValid()) {
					message = message.replace(item, "{" + i + "}");
					continue;
				}
				if (item.startsWith("```") && blockCount > 1) {
					inBlockComment = true;
					message = message.replace(item, "{" + i + "}");
					blockCount--;
					if (item.endsWith("```")) {
						inBlockComment = false;
						blockCount--;
					}
				}
				if (inBlockComment) {
					if (item.endsWith("```")) {
						inBlockComment = false;
					}
					message = message.replace(item, "{" + i + "}");
					blockCount--;
				}
			}
	
			int inlineCodeCount = LilGUtil.countMatches(message, "`");
			if (inlineCodeCount > 1) {
				if (inlineCodeCount % 2 != 0) {
					for (int count = 0; count < inlineCodeCount; count++) {
						message = message.replace('`', reverse);
					}
				} else {
					message = message.replace('`', reverse);
				}
			}
			int underlineCount = LilGUtil.countMatches(message, "__");
			if (underlineCount > 1) {
				if (underlineCount % 2 != 0) {
					for (int count = 0; count < underlineCount; count++) {
						message = message.replace("__", underline + "");
					}
				} else {
					message = message.replace("__", underline + "");
				}
			}
			int boldCount = LilGUtil.countMatches(message, "**");
			if (boldCount > 1) {
				if (boldCount % 2 != 0) {
					for (int count = 0; count < boldCount; count++) {
						message = message.replace("**", bold + "");
					}
				} else {
					message = message.replace("**", bold + "");
				}
			}
			int italicsCount = LilGUtil.countMatches(message, "_");
			if (italicsCount > 1) {
				if (italicsCount % 2 != 0) {
					for (int count = 0; count < italicsCount; count++) {
						message = message.replace('_', italics);
					}
				} else {
					message = message.replace('_', italics);
				}
			}
			italicsCount = LilGUtil.countMatches(message, "*");
			if (italicsCount > 1) {
				if (italicsCount % 2 != 0) {
					for (int count = 0; count < italicsCount; count++) {
						message = message.replace('*', italics);
					}
				} else {
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