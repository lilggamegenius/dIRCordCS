using System.Collections.Generic;
using ChatSharp;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using GistsApi;
using Newtonsoft.Json;

namespace dIRCordCS.Config{
	#pragma warning disable 0649
	public class Configuration{
		public bool AutoSplitMessage = true;
		public ulong DiscordBotOwnerID = 0;
		public string DiscordToken = "<Missing discord token in config>";
		//public string[] GithubCreds = {string.Empty, string.Empty};
		[JsonIgnore] public GistClient GistClient;
		public string GithubGistOAuthToken = string.Empty;
		public bool IrcNickColor = false;

		public int MinutesOfInactivityToUpdate = 10;
		public string Nickname = "dIRCord";
		public string RealName = "dIRCord - Discord IRC Bridge";

		public ServerConfigs[] Servers ={new()};
		public string UserAgent = "dIRCordCS / 1.0 Discord IRC Bridge";
		public string UserName = "dIRCord";

		public class ServerConfigs{
			[JsonProperty("DiscordBotOwnerID")] private readonly ulong _discordBotOwnerId;
			[JsonProperty("discordToken")] private readonly string _discordToken;
			[JsonProperty("nickname")] private readonly string _nickname;
			[JsonProperty("realname")] private readonly string _realName;
			[JsonProperty("username")] private readonly string _userName;

			public Dictionary<string, string> AutoBan = new();
			public List<string> AutoSendCommands = new();
			public List<string> BanOnSight = new();
			[JsonIgnore] public BiDictionary<IrcChannel, DiscordChannel> ChannelMapObj = new();

			public BiDictionary<string, ulong> ChannelMapping = new();

			public ChannelConfigs ChannelOptions = new();
			[JsonIgnore] public DiscordClient DiscordClient;
			[JsonIgnore] public DiscordListener DiscordListener;

			public bool FloodProtection = true;
			public int FloodProtectionDelay = 1000;
			public bool IgnoreInvalidSsl = false;
			public string IRCBotOwnerHostmask = "<IRC Bot Owner Hostmask missing>";
			[JsonIgnore] public IrcClient IrcClient;
			[JsonIgnore] public IrcListener IrcListener;

			[JsonIgnore] public bool IRCReady, DiscordReady;
			[JsonIgnore] public IrcUser IrcSelf;

			[JsonIgnore] public Configuration MainConfig;

			public string Modes = "+B";

			public string NickservAccountName = "<Optional>";
			public string NickservPassword = "<Optional>";
			public int Port = 6667;

			public string Server = "<Server missing from config>";
			public string ServerPassword;
			public bool Ssl = false;
			[JsonIgnore]
			public string Nickname=>_nickname ?? MainConfig.Nickname;
			[JsonIgnore]
			public string UserName=>_userName ?? MainConfig.UserName;
			[JsonIgnore]
			public string RealName=>_realName ?? MainConfig.RealName;

			[JsonIgnore]
			public string DiscordToken=>_discordToken ?? MainConfig.DiscordToken;
			[JsonIgnore]
			public ulong DiscordBotOwnerID=>_discordBotOwnerId == default ? MainConfig.DiscordBotOwnerID : _discordBotOwnerId;
		}

		public class ChannelConfigs{
			public readonly Dictionary<string, DiscordChannelConfiguration> Discord = new();
			public readonly Dictionary<string, IRCChannelConfiguration> Irc = new();
		}
	}
}
