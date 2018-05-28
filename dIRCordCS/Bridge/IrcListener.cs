using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using Common.Logging;
using dIRCordCS.Config;
using IrcDotNet;
using IrcDotNet.Event;
using IrcDotNet.Message;
using IrcDotNet.Network;
using IrcDotNet.Target.Channel;
using IrcDotNet.Target.User;

namespace dIRCordCS.Bridge{
	public class IrcListener : Listener{
		private static readonly ILog Logger = LogManager.GetLogger<IrcListener>();

		public IrcListener(byte configId) : base(configId){
			Console.WriteLine("Starting to connect to {0} as {1}.", Config.server, Config.nickname);
			var client = Config.ircClient;
			client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
			client.Disconnected += IrcClient_Disconnected;
			client.Registered += IrcClient_Registered;
			// Wait until connection has succeeded or timed out.
			var registeredEvent = new ManualResetEventSlim(false);
			var connectedEvent = new ManualResetEventSlim(false);
			client.Connected += (sender2, e2)=>connectedEvent.Set();
			client.Registered += (sender2, e2)=>registeredEvent.Set();
			client.Connect(Config.server,
			               Config.port,
			               Config.SSL,
			               Config.nickservPassword != null
				               ? new IrcNickservUserRegistrationInfo{
					               NickName = Config.nickname,
					               NickservPassword = Config.nickservPassword,
					               UserName = Config.userName
				               }
				               : new IrcUserRegistrationInfo{
					               NickName = Config.nickname,
					               UserName = Config.userName
				               }
			              );
			if(!connectedEvent.Wait(10000)){
				Console.WriteLine("Connection to '{0}' timed out.", Config.server);
				throw new NetworkInformationException();
			}

			Console.Out.WriteLine("Now connected to '{0}'.", Config.server);
			if(!registeredEvent.Wait(10000)){
				Console.WriteLine("Could not register to '{0}'.", Config.server);
				return;
			}

			Console.Out.WriteLine("Now registered to '{0}' as '{1}'.", Config.server, Config.userName);
		}

		public override void Rehash(ref Configuration newConfig, ref Configuration oldConfig){
			//config.channelMapping = HashBiMap.create(config.channelMapping);
			List<string> channelsToJoin = new List<string>();
			List<string> channelsToJoinKeys = new List<string>();
			List<string> channelsToPart = new List<string>(oldConfig.channelMapping.Values);
			foreach(string channel in newConfig.channelMapping.Values){
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

			oldConfig.ircClient.Channels.Leave(channelsToPart, "Rehashing");
			for(int index = 0; index < channelsToJoin.Count; index++){
				if(channelsToJoinKeys[index] != null){
					oldConfig.ircClient.Channels.Join(new Tuple<string, string>(channelsToJoin[index], channelsToJoinKeys[index]));
				}
				else{
					oldConfig.ircClient.Channels.Join(channelsToJoin[index]);
				}
			}
		}

		private static void IrcClient_Registered(object sender, EventArgs e){
			var client = (IrcClient)sender;
			client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
			client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
			client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
			client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;
		}

		private static void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e){
			var localUser = (IrcLocalUser)sender;
			e.Channel.UserJoined -= IrcClient_Channel_UserJoined;
			e.Channel.UserLeft -= IrcClient_Channel_UserLeft;
			e.Channel.MessageReceived -= IrcClient_Channel_MessageReceived;
			e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;
			Console.WriteLine("You left the channel {0}.", e.Channel.Name);
		}

		private static void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e){
			var localUser = (IrcLocalUser)sender;
			e.Channel.UserJoined += IrcClient_Channel_UserJoined;
			e.Channel.UserLeft += IrcClient_Channel_UserLeft;
			e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
			e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;
			Console.WriteLine("You joined the channel {0}.", e.Channel.Name);
		}

		private static void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e){
			var channel = (IrcChannel)sender;
			Console.WriteLine("[{0}] Notice: {1}.", channel.Name, e.Text);
		}

		private static void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e){
			var channel = (IrcChannel)sender;
			if(e.Source is IrcUser){
				// Read message.
				Console.WriteLine("[{0}]({1}): {2}.", channel.Name, e.Source.Name, e.Text);
			}
			else{
				Console.WriteLine("[{0}]({1}) Message: {2}.", channel.Name, e.Source.Name, e.Text);
			}
		}

		private static void IrcClient_Channel_UserLeft(object sender, IrcChannelUserEventArgs e){
			var channel = (IrcChannel)sender;
			Console.WriteLine("[{0}] User {1} left the channel.", channel.Name, e.ChannelUser.NickName);
		}

		private static void IrcClient_Channel_UserJoined(object sender, IrcChannelUserEventArgs e){
			var channel = (IrcChannel)sender;
			Console.WriteLine("[{0}] User {1} joined the channel.", channel.Name, e.ChannelUser.NickName);
		}

		private static void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e){
			var localUser = (IrcLocalUser)sender;
			if(e.Source is IrcUser){
				// Read message.
				Console.WriteLine("({0}): {1}.", e.Source.Name, e.Text);
			}
			else{
				Console.WriteLine("({0}) Message: {1}.", e.Source.Name, e.Text);
			}
		}

		private static void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e){
			var localUser = (IrcLocalUser)sender;
			Console.WriteLine("Notice: {0}.", e.Text);
		}

		private static void IrcClient_Disconnected(object sender, EventArgs e){
			var client = (IrcClient)sender;
		}

		private static void IrcClient_Connected(object sender, EventArgs e){
			var client = (IrcClient)sender;
		}
	}
}
