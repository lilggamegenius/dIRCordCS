using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using Common.Logging;
using dIRCordCS.Config;
using dIRCordCS.Utils;

namespace dIRCordCS.ChatBridge{
	public class IrcListener : Listener{
		private static readonly ILog Logger = LogManager.GetLogger<IrcListener>();
		public IrcClient IrcClient;
		public IrcUser IrcSelf;

		public IrcListener(byte configId) : base(configId){
			IrcSelf = Config.IrcSelf = new IrcUser(null, Config.Nickname, Config.UserName, Config.ServerPassword, Config.RealName);
			IrcClient = Config.IrcClient = new IrcClient($"{Config.Server}:{Config.Port}", Config.IrcSelf, Config.Ssl);
			IrcClient.IgnoreInvalidSSL = Config.IgnoreInvalidSsl;
			IrcClient.RawMessageRecieved += OnRawMessageRecieved;
			IrcClient.RawMessageSent += OnRawMessageSent;
			IrcClient.ConnectionComplete += OnConnect;
			IrcClient.UserJoinedChannel += OnUserJoinedChannel;
			IrcClient.PrivateMessageRecieved += OnPrivateMessageRecieved;
			//ircClient.ChannelMessageRecieved += onChannelMessageRecieved;
			IrcClient.Error += OnError;
			IrcClient.ConnectAsync();
			AppDomain.CurrentDomain.ProcessExit += ExitHandler;
		}
		private async void OnChannelMessageRecieved(PrivateMessageEventArgs e){
			IrcChannel source = IrcClient.Channels[e.PrivateMessage.Source];
			if(!await Bridge.CommandHandler(this, source, e)){
				Logger.InfoFormat("Message from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
				await Bridge.SendMessage(e.PrivateMessage.Message, source, e.PrivateMessage.User, this, ConfigId);
			}
			else{
				Logger.InfoFormat("Command from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
			}
		}

		private void OnPrivateMessageRecieved(object sender, PrivateMessageEventArgs e){
			if(e.PrivateMessage.Message.IsCtcp()){
				OnCtcpMessageRecieved(e);
				return;
			}

			if(e.PrivateMessage.IsChannelMessage){
				OnChannelMessageRecieved(e);
				return;
			}

			Logger.InfoFormat("Message from {0}: {1}", e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
		}

		private void OnCtcpMessageRecieved(PrivateMessageEventArgs e){
			string message = e.PrivateMessage.Message.Substring(1, e.PrivateMessage.Message.Length - 2);
			string command = message.SplitMessage()[0];
			switch(command){
				case nameof(CtcpCommands.PING):
					IrcClient.SendNotice("PONG".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.FINGER):
					IrcClient.SendNotice($"{command} You ought to be arrested for fingering a bot!".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.USERINFO):
				case nameof(CtcpCommands.VERSION):
					IrcClient.SendNotice($"{command} {Program.Version}".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.CLIENTINFO):
					IrcClient.SendNotice($"{command} ".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.SOURCE):
					IrcClient.SendNotice($"{command} dIRCord - https://github.com/lilggamegenius/dIRCord".ToCtcp());
					break;
				case nameof(CtcpCommands.TIME):
					IrcClient.SendNotice($"{command} ".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.PAGE):
					IrcClient.SendNotice($"{command} ".ToCtcp(), e.PrivateMessage.Source);
					break;
				case nameof(CtcpCommands.AVATAR):
					IrcClient.SendNotice($"{command} ".ToCtcp(), e.PrivateMessage.Source);
					break;
			}

			Logger.InfoFormat("Received CTCP message: {0}", message);
		}

		private void OnUserJoinedChannel(object sender, ChannelUserEventArgs e){
			Logger.InfoFormat("User {0} Joined channel {1}", e.User.Hostmask, e.Channel.Name);
		}

		private void OnRawMessageRecieved(object sender, RawMessageEventArgs args){
			Logger.DebugFormat("<<< {0}", args.Message);
		}

		private void OnRawMessageSent(object sender, RawMessageEventArgs args){
			Logger.DebugFormat(">>> {0}", args.Message);
		}

		private void OnConnect(object sender, EventArgs e){
			Task.Run(()=>{
				foreach(string channel in Config.ChannelMapping.Keys){
					string[] channelValues = channel.Split(new[]{' '}, 1);
					IrcClient.JoinChannel(channelValues[0]);
				}

				int neededChannels = Config.ChannelMapping.Count;
				SpinWait.SpinUntil(()=>IrcClient.Channels.Count == neededChannels, TimeSpan.FromSeconds(neededChannels * 5));
				Config.IRCReady = true;
				Bridge.FillMap(ConfigId);
				Config.IrcClient.SendRawMessage("ns identify {0} {1}", Config.NickservAccountName, Config.NickservPassword);
			});
		}

		public override void Rehash(Configuration.ServerConfigs newConfig, Configuration.ServerConfigs oldConfig){
			//config.channelMapping = HashBiMap.create(config.channelMapping);
			List<string> channelsToJoin = new List<string>();
			List<string> channelsToJoinKeys = new List<string>();
			List<string> channelsToPart = new List<string>(oldConfig.ChannelMapping.Keys);
			foreach(string channel in newConfig.ChannelMapping.Keys){
				string[] channelValues = channel.Split(new[]{' '}, 1);
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
			IrcClient.Quit("Shutting down");
		}

		private void OnError(object sender, ErrorEventArgs e){
			Logger.Error(e.Error.Message, e.Error);
		}

		private enum CtcpCommands{ PING, FINGER, VERSION, USERINFO, CLIENTINFO, SOURCE, TIME, PAGE, AVATAR }
	}
}
