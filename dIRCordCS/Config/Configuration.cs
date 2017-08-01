using System.Collections.Generic;
using dIRCordCS.utils;
using dIRCordCS.Utils;
using Discord;
using Discord.WebSocket;
using org.pircbotx;

namespace dIRCordCS.Config{
	public struct Configuration{
		public string nickname;// = "dIRCord";
		public string userName;// = "dIRCord";
		public string realName;// = "dIRCord - Discord IRC Bridge";
		public string server;// = "<Missing server in config>";
		public int port;// = 6667;
		public bool SSL;// = false;
		public string nickservPassword;// = "<Missing nickserv password in config>";
		public bool autoSplitMessage;// = false;
		public List<string> autoSendCommands;// = new List<string>();
		public bool floodProtection;// = true;
		public int floodProtectionDelay;// = 1000;
		public bool ircNickColor;// = false;
		public string discordToken;// = "<Missing discord token in config>";

		public BiDictionary<string, string> channelMapping;// = HashBiMap.create();

		public ChannelConfigs channelOptions;// = new ChannelConfigs();

		public int minutesOfInactivityToUpdate;// = 10;

		[ScriptIgnore] public BiDictionary<ITextChannel, Channel> channelMapObj;// = HashBiMap.create();
		[ScriptIgnore] public IrcListener ircListener;
		[ScriptIgnore] public DiscordListener discordListener;
		[ScriptIgnore] public PircBotX pircBotX;
		[ScriptIgnore] public DiscordSocketClient discordSocketClient;

		public struct ChannelConfigs {
			public readonly Dictionary<string, DiscordChannelConfiguration> Discord;
			public readonly Dictionary<string, IRCChannelConfiguration> IRC;
		}
	}
}