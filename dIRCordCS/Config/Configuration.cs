using System;
using System.Collections.Generic;
using dIRCordCS.Listeners;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
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

		public Dictionary<string, string> AutoBan;
		public List<string> BanOnSight;

		[JsonIgnore] public BiDictionary<DiscordChannel, Channel> channelMapObj;// = HashBiMap.create();
		[JsonIgnore] public IrcListener ircListener;
		[JsonIgnore] public DiscordListener discordListener;
		[JsonIgnore] public PircBotX pircBotX;
		[JsonIgnore] public DiscordClient discordSocketClient;

		public struct ChannelConfigs {
			public readonly Dictionary<string, DiscordChannelConfiguration> Discord;
			public readonly Dictionary<string, IRCChannelConfiguration> IRC;
		}

		public static bool operator==(Configuration conf1, Configuration conf2){
			return conf1.Equals(conf2);
		}
		public static bool operator!=(Configuration conf1, Configuration conf2){
			return !(conf1 == conf2);
		}
		public bool Equals(Configuration other){
			return string.Equals(nickname, other.nickname, StringComparison.OrdinalIgnoreCase) && string.Equals(userName, other.userName, StringComparison.OrdinalIgnoreCase) && string.Equals(realName, other.realName, StringComparison.OrdinalIgnoreCase) && string.Equals(server, other.server, StringComparison.OrdinalIgnoreCase) && port == other.port && SSL == other.SSL && string.Equals(nickservPassword, other.nickservPassword, StringComparison.OrdinalIgnoreCase) && autoSplitMessage == other.autoSplitMessage && Equals(autoSendCommands, other.autoSendCommands) && floodProtection == other.floodProtection && floodProtectionDelay == other.floodProtectionDelay && ircNickColor == other.ircNickColor && string.Equals(discordToken, other.discordToken, StringComparison.OrdinalIgnoreCase) && Equals(channelMapping, other.channelMapping) && channelOptions.Equals(other.channelOptions) && minutesOfInactivityToUpdate == other.minutesOfInactivityToUpdate && Equals(AutoBan, other.AutoBan) && Equals(BanOnSight, other.BanOnSight) && Equals(channelMapObj, other.channelMapObj) && Equals(ircListener, other.ircListener) && Equals(discordListener, other.discordListener) && Equals(pircBotX, other.pircBotX) && Equals(discordSocketClient, other.discordSocketClient);
		}
		public override bool Equals(object obj){
			if(ReferenceEquals(null, obj))
				return false;
			return obj is Configuration && Equals((Configuration)obj);
		}
		public override int GetHashCode(){
			unchecked{
				var hashCode = (nickname != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(nickname) : 0);
				hashCode = (hashCode * 397) ^ (userName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(userName) : 0);
				hashCode = (hashCode * 397) ^ (realName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(realName) : 0);
				hashCode = (hashCode * 397) ^ (server != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(server) : 0);
				hashCode = (hashCode * 397) ^ port;
				hashCode = (hashCode * 397) ^ SSL.GetHashCode();
				hashCode = (hashCode * 397) ^ (nickservPassword != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(nickservPassword) : 0);
				hashCode = (hashCode * 397) ^ autoSplitMessage.GetHashCode();
				hashCode = (hashCode * 397) ^ (autoSendCommands != null ? autoSendCommands.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ floodProtection.GetHashCode();
				hashCode = (hashCode * 397) ^ floodProtectionDelay;
				hashCode = (hashCode * 397) ^ ircNickColor.GetHashCode();
				hashCode = (hashCode * 397) ^ (discordToken != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(discordToken) : 0);
				hashCode = (hashCode * 397) ^ (channelMapping != null ? channelMapping.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ channelOptions.GetHashCode();
				hashCode = (hashCode * 397) ^ minutesOfInactivityToUpdate;
				hashCode = (hashCode * 397) ^ (AutoBan != null ? AutoBan.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (BanOnSight != null ? BanOnSight.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (channelMapObj != null ? channelMapObj.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ircListener != null ? ircListener.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (discordListener != null ? discordListener.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (pircBotX != null ? pircBotX.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (discordSocketClient != null ? discordSocketClient.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}
