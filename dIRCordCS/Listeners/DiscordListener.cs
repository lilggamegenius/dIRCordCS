using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using org.pircbotx;
using Configuration = dIRCordCS.Config.Configuration;
using static dIRCordCS.Bridge;
using static dIRCordCS.Utils.LilGUtil;

namespace dIRCordCS.Listeners{
	public class DiscordListener{
		public static readonly char colorCode = '\u0003';
		public static readonly char zeroWidthSpace = '\u200B';
		private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
		public DiscordClient client;
		private readonly byte configID;
		public volatile bool ready;

		public DiscordListener(byte configID){
			this.configID = configID;
			client = Program.Config[configID].discordSocketClient;
		}

		private string formatMember(DiscordMember user, FormatAs format = FormatAs.effectiveName) {
			switch (format) {
				case FormatAs.effectiveName:
					return formatMember(user, null);
				case FormatAs.NickName:
					return formatMember(user, user.Nickname);
				case FormatAs.Username:
					return formatMember(user, user.Username);
				case FormatAs.ID:
					return formatMember(user, user.Id.ToString()); // ????
			}
			return "";
		}

		private string formatMember(DiscordMember user, string @override) {
			if (@override == null) {
				@override = user.DisplayName;
			}
			string color = "";
			if (config().ircNickColor) {
				int ircColorCode = ColorMap[user.Color].GetIRCColor();
				if (ircColorCode < 0) {
					ircColorCode = user.DisplayName.hash(12) + 2;
				}
				color = colorCode + $"{ircColorCode:D2}";
			}
			string nameWithSpace = @override[0] + zeroWidthSpace + @override.Substring(1);
			return $"{color}{nameWithSpace}{colorCode}";
		}

		private Channel getIRCChannel(DiscordChannel channel) {
			return config().channelMapObj[channel];
		}


		public async void onReady(ReadyEventArgs @event) {
			ready = true;
			config().ircListener.fillChannelMap();
			Dictionary<string, DiscordChannelConfiguration> configs = config().channelOptions.Discord;
			await Task.Run(()=>{
				foreach(DiscordChannel channel in config().channelMapObj.Keys){
					if(!configs.ContainsKey("#" + channel.Name)){
						configs["#" + channel.Name] = new DiscordChannelConfiguration();
					}
				}
			});
		}

		/*private Channel getIRCChannel(MessageCreateEventArgs @event) {
			return config().channelMapObj.get(@event.getChannel());
		}*/

		private bool handleCommand(MessageCreateEventArgs @event) {
			string[] message = LilGUtil.splitMessage(@event.Message.Content);
			if (string.IsNullOrWhiteSpace(message[0])) {
				return false;
			}
			if (message[0].StartsWith(@event.Guild.CurrentMember.DisplayName) ||
					message[0].StartsWith(@event.Guild.CurrentMember.Mention)
					) {
				if (getIRCChannel(@event.Channel) != null) {
					Bridge.handleCommand(message, @event, configID, false);
					return true;
				}
			}
			return false;
		}

		public async void onGuildMessageReceived(MessageCreateEventArgs @event) {
			Program.LastActivity = Program.CurrentTimeMillis;
			DiscordMember member = await @event.Guild.GetMemberAsync(@event.Author.Id);
			try {
				string discordNick = "Error nick";
				string discordUsername = "Error UserName";
				string discordHostmask = "Error Hostmask";
				try{
					discordNick = member.DisplayName;
					discordUsername = member.Username;
					discordHostmask = member.Id.ToString();
				} catch (Exception e) {
					Logger.Error("Error receiving message" + Program.ErrorMsg, e);
				}
				Logger.InfoFormat("#{0}: <{1}!{2}@{3}> {4}", @event.Channel.Name, discordNick, discordUsername, discordHostmask, @event.Message.Content);

				if (member.IsBot ||
						@event.Author.Equals(client.CurrentUser) ||
						handleCommand(@event)) {
					return;
				}
				string message = @event.Message.Content;
				Channel channel = getIRCChannel(@event.Channel);
				if (channel == null) {
					return;
				}
				if (message.Length != 0) {
					DiscordChannelConfiguration configuration = channelConfig(@event.Channel);
					if (message.startsWithAny(configuration.getCommmandCharacters())) {
						channel.send().message($"\u001DCommand Sent by\u001D \u0002{formatMember(member)}\u0002");
						channel.send().message(formatString(@event.Channel, @event.Message.Content));
					} else {
						string user = formatMember(member);
						string msg = formatString(@event.Channel, @event.Message.Content);
						Dictionary<string, string> autoBan = config().AutoBan;
						if (autoBan.Keys.Count != 0) {
							foreach(string match in autoBan.Keys) {
								if (LilGUtil.wildCardMatch(msg, match)) {
									string reason = autoBan[match];
									Task.Run(async ()=>{
										await member.SendMessageAsync();
										await member.SendMessageAsync("You were banned: " + reason);
										@event.Guild.BanMemberAsync(member, 0, reason);
									});
									@event.Message.DeleteAsync(reason);
									return;
								}
							}
						}
						List<string> spamFilterList = config().channelOptions.IRC[channel.getName()].spamFilterList;
						if (spamFilterList.Count != 0) {
							foreach(string match in spamFilterList) {
								if (LilGUtil.wildCardMatch(msg, match)) {
									@event.Message.DeleteAsync("in spam filter list of " + channel.getName());
									return;
								}
							}
						}
						//:<hostmask> PRIVMSG #<channel> :<msg>\r\n
						string msgLen = ":" + channel.getBot().getUserBot().getHostmask() + " PRIVMSG " + channel.getName() + " :" + message;
						if (msgLen.Length > 500) {
							int hostMaskLen = channel.getBot().getUserBot().getHostmask().Length;
							int channelLen = channel.getName().Length;
							foreach(string str in msg.SplitUp(490 - (user.Length + hostMaskLen + channelLen))) {
								channel.send().message($"<{user}> {str}");
							}
						} else {
							if (msg.StartsWith("\0")) {
								channel.send().message($"*{user}* {msg.Substring(1)}");
							} else {
								channel.send().message($"<{user}> {msg}");
							}
						}
					}
				}
				IReadOnlyList<DiscordAttachment> attachments;
				if ((attachments = @event.Message.Attachments).Count != 0) {
					StringBuilder embedMessage = new StringBuilder($"Attachments from <{formatMember(member)}>:");
					foreach(DiscordAttachment attachment in attachments) {
						embedMessage.Append(" ").Append(attachment.Url);
					}
					channel.send().message(embedMessage.ToString());
				}
				Program.LastUserToSpeak[@event.Channel] = member;
			} catch (Exception e) {
				Logger.Error("Error in DiscordListener" + Program.ErrorMsg, e);
			}
		}


		public void onTextChannelUpdateTopic(ChannelUpdateEventArgs @event) {
			Channel channel = getIRCChannel(@event.ChannelAfter);

			// Afaik Discord doesn't have info for who changed a topic
			channel?.send().message(string.Format("{0} has changed topic to: {1}", "A user", @event.ChannelAfter.Topic));
		}

		/*public void onUserOnlineStatusUpdate(UserOnlineStatusUpdateEvent @event) {
			if (@event.getUser().equals(jda.getSelfUser())) {
				return;
			}
			DiscordMember member = @event.getGuild().getMember(@event.getUser());
			for (string discordChannels : config().channelMapping.Keys) {
				for(DiscordChannel textChannel : @event.getGuild().getTextChannels()) {
					if (discordChannels.substring(1, discordChannels.length()).equals(textChannel.Name)) {
						string color = "";
						if (config().ircNickColor) {
							int ircColorCode = ColorMap.valueOf(member.getColor());
							if (ircColorCode < 0) {
								ircColorCode = LilGUtil.hash(member.DisplayName, 12) + 2;
							}
							color = colorCode + string.format("%02d", ircColorCode);
						}

						Program.pircBotX.send()
								.message(config()
												.channelMapping
												.get(discordChannels),
										string.format("\\*%s%s%c\\* %s",
												color,
												prevNick,
												colorCode,
												"Has changed nick to " + newNick
										)
								)
						;
					}
				}
			}
		}*/

		public async void onGuildMemberNickChange(GuildMemberUpdateEventArgs @event) {
			DiscordMember user = @event.Member;
			DiscordMember self = @event.Guild.CurrentMember;
			bool same = false;
			if (user.Equals(self)) {
				return;
			}
			if (user.DisplayName.Equals(self.DisplayName, StringComparison.OrdinalIgnoreCase)) {
				if (self.GetPermissions().HasPermission(Permissions.ChangeNickname) && self.CanInteract(user)) {
					user.ModifyAsync(@event.NicknameBefore, reason:"Same nick as bridge bot");
				} else {
					same = true;
					@event.Guild.GetDefaultChannel()
					      .SendMessageAsync($"User {user.GetHostMask()} has the same name as the bridge bot");
				}
			}
			Channel channel = null;
			DiscordChannel textChannel1 = null;
			foreach(DiscordChannel textChannel in @event.Guild.Channels.Where(chan => chan.Type == ChannelType.Text)) {
				channel = config().channelMapObj[textChannel];
				if (channel == null) {
					continue;
				}
				textChannel1 = textChannel;

				string prevNick = @event.NicknameBefore;
				string newNick = @event.NicknameAfter;
				string username = @event.Member.Username;
				if (prevNick == null) {
					prevNick = username;
				} else if (newNick == null) {
					newNick = username;
				}
				channel.send().message($"\u001D*{formatMember(user, prevNick)}\u001D* Has changed nick to {formatMember(user, newNick)}{(same ? " And now shares the name with the bridge bot" : "")}");
			}
			string hostmask = user.GetHostMask();
			foreach(string masksToBan in config().BanOnSight){
				if(hostmask.MatchHostMask(masksToBan)){
					await Task.Run(async ()=>{
						await user.BanAsync(0, "Ban On Sight: " + masksToBan);
						await textChannel1.SendMessageAsync(string.Format("User {0} was banned due to being on Ban-On-Sight list", hostmask));
						if(channel != null) {
							channel.send().message($"\u001D*{formatMember(user, user.DisplayName)}\u001D* was banned due to being on Ban-On-Sight list");
						}
					});
					return;
				}
			}
		}

		public async void onGuildMemberJoin(GuildMemberAddEventArgs @event){
			DiscordMember user = @event.Member;
			Channel channel = null;
			DiscordChannel textChannel = null;
			foreach(DiscordChannel textChannel2 in @event.Guild.Channels.Where(chan=>chan.Type == ChannelType.Text)) {
				channel = config().channelMapObj[textChannel2];
				textChannel = textChannel2;
				if (channel != null) {
					break;
				}
			}
			if (channel == null) {
				return;
			}
			string hostmask = user.GetHostMask();
			foreach(string masksToBan in config().BanOnSight){
				if(hostmask.MatchHostMask(masksToBan)){
					await user.BanAsync(0, "Ban On Sight: " + masksToBan);
					await textChannel.SendMessageAsync(string.Format("User {0} was banned due to being on Ban-On-Sight list", hostmask));
					channel.send().message($"\u001D*{formatMember(user, user.DisplayName)}\u001D* was banned due to being on Ban-On-Sight list");
				}
			}
		}

		private Configuration config() {
			return Program.Config[configID];
		}

		private DiscordChannelConfiguration channelConfig(DiscordChannel channel) {
			return channelConfig(channel.Name);
		}

		private DiscordChannelConfiguration channelConfig(string channel) {
			return config().channelOptions.Discord["#" + channel];
		}

		enum FormatAs {
			effectiveName, NickName, Username, ID
		}

		public static ColorMappings ColorMap = new ColorMappings();


	}

	public class ColorMappings {
			public static readonly Dictionary<Color, (byte, DiscordColor)> colorDictionary = new Dictionary<Color, (byte, DiscordColor)>{
				{Color.Turquoise,		(10, new DiscordColor(26, 188, 156))},
				{Color.DarkTurquoise,	(10, new DiscordColor(17, 128, 106))},
				{Color.Green,			(9,	new DiscordColor(46, 204, 113))},
				{Color.DarkGreen,		(3,	new DiscordColor(31, 139, 76))},
				{Color.Blue,			(10, new DiscordColor(52, 152, 219))},
				{Color.DarkBlue,		(2, new DiscordColor(32, 102, 148))},
				{Color.Purple,			(13, new DiscordColor(155, 89, 182))},
				{Color.DarkPurple,		(6, new DiscordColor(113, 54, 138))},
				{Color.Pink,			(13, new DiscordColor(233, 30, 99))},
				{Color.DarkPink,		(6, new DiscordColor(173, 20, 87))},
				{Color.Yellow,			(8, new DiscordColor(241, 196, 15))},
				{Color.DarkYellow,		(8, new DiscordColor(194, 124, 14))},
				{Color.Orange,			(7, new DiscordColor(230, 126, 34))},
				{Color.DarkOrange,		(7, new DiscordColor(168, 67, 0))},
				{Color.Red,				(4, new DiscordColor(231, 76, 60))},
				{Color.DarkRed,			(5, new DiscordColor(153, 45, 34))},
				{Color.LightGray,		(0, new DiscordColor(149, 165, 166))},
				{Color.Gray,			(15, new DiscordColor(151, 156, 159))},
				{Color.DarkGray,		(14, new DiscordColor(96, 125, 139))},
				{Color.DarkerGray,		(1, new DiscordColor(84, 110, 122))}
			};

			public Color this[byte number]{
				get{
					foreach(Color color in colorDictionary.Keys){
						if(colorDictionary[color].Item1 == number)
							return color;
					}

					return 0;
				}
			}

			public Color this[DiscordColor findDiscordColor]{
				get{
					foreach(Color color in colorDictionary.Keys){
						if(colorDictionary[color].Item2.Equals(findDiscordColor))
							return color;
					}

					return 0;
				}
			}

			public enum Color : byte{
				Turquoise = 1,
				DarkTurquoise,
				Green,
				DarkGreen,
				Blue,
				DarkBlue,
				Purple,
				DarkPurple,
				Pink,
				DarkPink,
				Yellow,
				DarkYellow,
				Orange,
				DarkOrange,
				Red,
				DarkRed,
				LightGray,
				Gray,
				DarkGray,
				DarkerGray,
			}
		}
}
