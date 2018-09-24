using System;
using System.Collections.Generic;
using ChatSharp;
using dIRCordCS.ChatBridge;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace dIRCordCS.Config{
	public struct Configuration{
		public string Nickname; // = "dIRCord";
		public string UserName; // = "dIRCord";
		public string RealName; // = "dIRCord - Discord IRC Bridge";
		public string Server;   // = "<Missing server in config>";
		public string ServerPassword;
		public int Port; // = 6667;
		public bool Ssl; // = false;
		public bool IgnoreInvalidSsl;
		public string NickservPassword;       // = "<Missing nickserv password in config>";
		public bool AutoSplitMessage;         // = false;
		public List<string> AutoSendCommands; // = new List<string>();
		public bool FloodProtection;          // = true;
		public int FloodProtectionDelay;      // = 1000;
		public bool IrcNickColor;             // = false;
		public string DiscordToken;           // = "<Missing discord token in config>";
		public string IRCBotOwnerHostmask;
		public ulong DiscordBotOwnerID;
		public string GithubGistOAuthToken;
		public string[] GithubCreds; //

		public BiDictionary<string, ulong> ChannelMapping; // = HashBiMap.create();

		public ChannelConfigs ChannelOptions; // = new ChannelConfigs();

		public int MinutesOfInactivityToUpdate; // = 10;

		public Dictionary<string, string> AutoBan;
		public List<string> BanOnSight;

		[JsonIgnore] public bool IRCReady, DiscordReady;
		[JsonIgnore] public BiDictionary<IrcChannel, DiscordChannel> ChannelMapObj; // = HashBiMap.create();
		[JsonIgnore] public IrcListener IrcListener;
		[JsonIgnore] public DiscordListener DiscordListener;
		[JsonIgnore] public IrcClient IrcClient;
		[JsonIgnore] public IrcUser IrcSelf;
		[JsonIgnore] public DiscordClient DiscordSocketClient;

		public struct ChannelConfigs{
			public readonly Dictionary<string, DiscordChannelConfiguration> Discord;
			public readonly Dictionary<string, IRCChannelConfiguration> Irc;
		}

		public static bool operator==(Configuration conf1, Configuration conf2)=>conf1.Equals(conf2);
		public static bool operator!=(Configuration conf1, Configuration conf2)=>!(conf1 == conf2);
		public bool Equals(Configuration other)=>string.Equals(Nickname, other.Nickname, StringComparison.OrdinalIgnoreCase)                 &&
												 string.Equals(UserName, other.UserName, StringComparison.OrdinalIgnoreCase)                 &&
												 string.Equals(RealName, other.RealName, StringComparison.OrdinalIgnoreCase)                 &&
												 string.Equals(Server, other.Server, StringComparison.OrdinalIgnoreCase)                     &&
												 (Port == other.Port)                                                                        &&
												 (Ssl  == other.Ssl)                                                                         &&
												 string.Equals(NickservPassword, other.NickservPassword, StringComparison.OrdinalIgnoreCase) &&
												 (AutoSplitMessage == other.AutoSplitMessage)                                                &&
												 Equals(AutoSendCommands, other.AutoSendCommands)                                            &&
												 (FloodProtection      == other.FloodProtection)                                             &&
												 (FloodProtectionDelay == other.FloodProtectionDelay)                                        &&
												 (IrcNickColor         == other.IrcNickColor)                                                &&
												 string.Equals(DiscordToken, other.DiscordToken, StringComparison.OrdinalIgnoreCase)         &&
												 Equals(ChannelMapping, other.ChannelMapping)                                                &&
												 ChannelOptions.Equals(other.ChannelOptions)                                                 &&
												 (MinutesOfInactivityToUpdate == other.MinutesOfInactivityToUpdate)                          &&
												 Equals(AutoBan, other.AutoBan)                                                              &&
												 Equals(BanOnSight, other.BanOnSight)                                                        &&
												 Equals(ChannelMapObj, other.ChannelMapObj)                                                  &&
												 Equals(IrcListener, other.IrcListener)                                                      &&
												 Equals(DiscordListener, other.DiscordListener)                                              &&
												 Equals(IrcClient, other.IrcClient)                                                          &&
												 Equals(DiscordSocketClient, other.DiscordSocketClient);
		public override bool Equals(object obj){
			if(ReferenceEquals(null, obj)){ return false; }

			return obj is Configuration configuration && Equals(configuration);
		}
		public override int GetHashCode(){
			unchecked{
				int hashCode = Nickname                 != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Nickname) : 0;
				hashCode = (hashCode * 397) ^ (UserName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(UserName) : 0);
				hashCode = (hashCode * 397) ^ (RealName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(RealName) : 0);
				hashCode = (hashCode * 397) ^ (Server   != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Server) : 0);
				hashCode = (hashCode * 397) ^ Port;
				hashCode = (hashCode * 397) ^ Ssl.GetHashCode();
				hashCode = (hashCode * 397) ^ (NickservPassword != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(NickservPassword) : 0);
				hashCode = (hashCode * 397) ^ AutoSplitMessage.GetHashCode();
				hashCode = (hashCode * 397) ^ (AutoSendCommands != null ? AutoSendCommands.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ FloodProtection.GetHashCode();
				hashCode = (hashCode * 397) ^ FloodProtectionDelay;
				hashCode = (hashCode * 397) ^ IrcNickColor.GetHashCode();
				hashCode = (hashCode * 397) ^ (DiscordToken   != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(DiscordToken) : 0);
				hashCode = (hashCode * 397) ^ (ChannelMapping != null ? ChannelMapping.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ChannelOptions.GetHashCode();
				hashCode = (hashCode * 397) ^ MinutesOfInactivityToUpdate;
				hashCode = (hashCode * 397) ^ (AutoBan             != null ? AutoBan.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (BanOnSight          != null ? BanOnSight.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ChannelMapObj       != null ? ChannelMapObj.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (IrcListener         != null ? IrcListener.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (DiscordListener     != null ? DiscordListener.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (IrcClient           != null ? IrcClient.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (DiscordSocketClient != null ? DiscordSocketClient.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}
