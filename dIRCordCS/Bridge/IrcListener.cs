using System;
using System.Collections.Generic;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.Config;
using dIRCordCS.Utils;

namespace dIRCordCS.Bridge{
	public class IrcListener : Listener{
		private static readonly ILog Logger = LogManager.GetLogger<IrcListener>();
		private IrcUser ircSelf;
		private IrcClient ircClient;

		public IrcListener(byte configId) : base(configId){
			ircSelf = Config.IrcSelf = new IrcUser(Config.Nickname, Config.UserName, Config.ServerPassword, Config.RealName);
			ircClient = Config.IrcClient = new IrcClient($"{Config.Server}:{Config.Port}", Config.IrcSelf, Config.Ssl);
			ircClient.IgnoreInvalidSSL = Config.IgnoreInvalidSsl;
			ircClient.RawMessageRecieved += onRawMessageRecieved;
			ircClient.RawMessageSent += onRawMessageSent;
			ircClient.ConnectionComplete += onConnect;
			ircClient.UserJoinedChannel += onUserJoinedChannel;
			ircClient.PrivateMessageRecieved += onPrivateMessageRecieved;
			//ircClient.ChannelMessageRecieved += onChannelMessageRecieved;
			ircClient.ConnectAsync();
			AppDomain.CurrentDomain.ProcessExit += ExitHandler;
		}
		private void onChannelMessageRecieved(object sender, PrivateMessageEventArgs e){
			Logger.DebugFormat("Message from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
		}

		private void onPrivateMessageRecieved(object sender, PrivateMessageEventArgs e){
			if(e.PrivateMessage.Message.IsCTCP()){
				onCtcpMessageRecieved(e);
				return;
			}
			if(e.PrivateMessage.IsChannelMessage){
				Logger.DebugFormat("Message from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
			}
			else{
				Logger.DebugFormat("Message from {0}: {1}", e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
			}
		}

		private void onCtcpMessageRecieved(PrivateMessageEventArgs e){
			string message = e.PrivateMessage.Message.Substring(1, e.PrivateMessage.Message.Length - 2);
			switch(message){
				case "PING":
				ircClient.SendNotice("PONG".ToCTCP());
				break;
				case "FINGER":
				ircClient.SendNotice("You ought to be arrested for fingering a bot!".ToCTCP());
				break;
				case "VERSION":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "USERINFO":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "CLIENTINFO":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "SOURCE":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "TIME":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "PAGE":
				ircClient.SendNotice("".ToCTCP());
				break;
				case "AVATAR":
				ircClient.SendNotice("".ToCTCP());
				break;
			}
			Logger.DebugFormat("Recieved CTCP message: {0}", message);
		}

		private void onUserJoinedChannel(object sender, ChannelUserEventArgs e){
			Logger.DebugFormat("User {0} Joined channel {1}", e.User.Hostmask, e.Channel.Name);
		}

		private void onRawMessageRecieved(object sender, RawMessageEventArgs args){
			Logger.DebugFormat("<<< {0}", args.Message);
		}

		private void onRawMessageSent(object sender, RawMessageEventArgs args){
			Logger.DebugFormat(">>> {0}", args.Message);
		}

		private void onConnect(object sender, EventArgs e){
			foreach(string channel in Config.ChannelMapping.Values){
				String[] channelValues = channel.Split(new[]{' '}, 1);
				ircClient.Channels.Join(channelValues[0]);
			}
		}

		public override void Rehash(ref Configuration newConfig, ref Configuration oldConfig){
			//config.channelMapping = HashBiMap.create(config.channelMapping);
			List<string> channelsToJoin = new List<string>();
			List<string> channelsToJoinKeys = new List<string>();
			List<string> channelsToPart = new List<string>(oldConfig.ChannelMapping.Values);
			foreach(string channel in newConfig.ChannelMapping.Values){
				String[] channelValues = channel.Split(new[]{' '}, 1);
				if(!channelsToPart.Remove(channelValues[0])){
					channelsToJoin.Add(channelValues[0]);
					if(channelValues.Length > 1){
						channelsToJoinKeys.Add(channelValues[1]);
					}
					else{
						channelsToJoinKeys.Add(null);
					}
				}
			}

			foreach(IrcChannel channel in oldConfig.IrcClient.Channels){
				if(channelsToPart.Contains(channel.Name)){
					channel.Part("Rehashing");
				}
			}

			for(int index = 0; index < channelsToJoin.Count; index++){
				if(channelsToJoinKeys[index] != null){
					throw new NotImplementedException("Joinning channels with passwords is not implimented yet");
					//oldConfig.ircClient.Channels.Join(channelsToJoin[index], channelsToJoinKeys[index]);
				}
				// ReSharper disable once RedundantIfElseBlock
				else{
					oldConfig.IrcClient.Channels.Join(channelsToJoin[index]);
				}
			}
		}

		protected override void ExitHandler(object sender, EventArgs args){
			ircClient.Quit("Shutting down");
		}
	}
}
