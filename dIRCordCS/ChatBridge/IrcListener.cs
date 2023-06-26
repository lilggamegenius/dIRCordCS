using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatSharp;
using ChatSharp.Events;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using NLog;

namespace dIRCordCS.ChatBridge;

public class IrcListener : Listener{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	public readonly IrcClient IrcClient;
	public readonly IrcUser IrcSelf;

	public IrcListener(byte configId) : base(configId){
		IrcSelf = Config.IrcSelf = new IrcUser(null, Config.Nickname, Config.UserName, Config.ServerPassword, Config.RealName);
		IrcClient = Config.IrcClient = new IrcClient($"{Config.Server}:{Config.Port}", Config.IrcSelf, Config.Ssl);
		IrcClient.IgnoreInvalidSSL = Config.IgnoreInvalidSsl;
		IrcClient.RawMessageReceived += OnRawMessageReceived;
		IrcClient.RawMessageSent += OnRawMessageSent;
		IrcClient.ConnectionComplete += OnConnect;
		IrcClient.UserJoinedChannel += OnUserJoinedChannel;
		IrcClient.PrivateMessageReceived += OnPrivateMessageReceived;
		//ircClient.ChannelMessageReceived += onChannelMessageReceived;
		IrcClient.Error += OnError;
		IrcClient.ConnectAsync();
		AppDomain.CurrentDomain.ProcessExit += ExitHandler;
	}
	private async void OnChannelMessageReceived(PrivateMessageEventArgs e){
		IrcChannel source = IrcClient.Channels[e.PrivateMessage.Source];
		if(!await Bridge.CommandHandler(this, source, e)){
			Logger.Info("Message from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
			await Bridge.SendMessage(e.PrivateMessage.Message, source, e.PrivateMessage.User, this, ConfigId);
		} else{
			Logger.Info("Command from {0} by {1}: {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
		}
	}

	private void OnPrivateMessageReceived(object sender, PrivateMessageEventArgs e){
		if(e.PrivateMessage.Message.IsCtcp()){
			OnCtcpMessageReceived(e);
			return;
		}

		if(e.PrivateMessage.IsChannelMessage){
			OnChannelMessageReceived(e);
			return;
		}

		Logger.Info("Message from {0}: {1}", e.PrivateMessage.User.Hostmask, e.PrivateMessage.Message);
	}

	private async void OnCtcpMessageReceived(PrivateMessageEventArgs e){
		string message = e.PrivateMessage.Message.Substring(1, e.PrivateMessage.Message.Length - 2);
		string command = message.SplitMessage()[0];
		switch(command){
			case nameof(CtcpCommands.PING):
				await IrcClient.SendNoticeAsync("PONG".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.FINGER):
				await IrcClient.SendNoticeAsync($"{command} You ought to be arrested for fingering a bot!".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.USERINFO):
			case nameof(CtcpCommands.VERSION):
				await IrcClient.SendNoticeAsync($"{command} {Program.Version}".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.CLIENTINFO):
				await IrcClient.SendNoticeAsync($"{command} ".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.SOURCE):
				await IrcClient.SendNoticeAsync($"{command} dIRCord - https://github.com/lilggamegenius/dIRCord".ToCtcp());
				break;
			case nameof(CtcpCommands.TIME):
				await IrcClient.SendNoticeAsync($"{command} ".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.PAGE):
				await IrcClient.SendNoticeAsync($"{command} ".ToCtcp(), e.PrivateMessage.Source);
				break;
			case nameof(CtcpCommands.AVATAR):
				await IrcClient.SendNoticeAsync($"{command} ".ToCtcp(), e.PrivateMessage.Source);
				break;
		}

		Logger.Info("Received CTCP message: {0}", message);
	}

	private static void OnUserJoinedChannel(object sender, ChannelUserEventArgs e)=>Logger.Info("User {0} Joined channel {1}", e.User.Hostmask, e.Channel.Name);

	private static void OnRawMessageReceived(object sender, RawMessageEventArgs args)=>Logger.Debug("<<< {0}", args.Message);

	private static void OnRawMessageSent(object sender, RawMessageEventArgs args)=>Logger.Debug(">>> {0}", args.Message);

	private async void OnConnect(object sender, EventArgs e){
		List<Task> joinChannelTasks = new();
		foreach(string channel in Config.ChannelMapping.Keys){
			string[] channelValues = channel.Split(new[]{ ' ' }, 1);
			joinChannelTasks.Add(IrcClient.JoinChannelAsync(channelValues[0]));
		}

		await Task.WhenAll(joinChannelTasks);
		int neededChannels = Config.ChannelMapping.Count;
		SpinWait.SpinUntil(()=>IrcClient.Channels.Count == neededChannels, TimeSpan.FromSeconds(neededChannels * 5));
		Config.IRCReady = true;
		Bridge.FillMap(ConfigId);
		await Config.IrcClient.SendRawMessageAsync("ns identify {0} {1}", Config.NickservAccountName, Config.NickservPassword);
	}

	public override async void Rehash(Configuration.ServerConfigs newConfig, Configuration.ServerConfigs oldConfig){
		//config.channelMapping = HashBiMap.create(config.channelMapping);
		List<string> channelsToJoin = new();
		List<string> channelsToJoinKeys = new();
		List<string> channelsToPart = new(oldConfig.ChannelMapping.Keys);
		foreach(string channel in newConfig.ChannelMapping.Keys){
			string[] channelValues = channel.Split(new[]{ ' ' }, 1);
			if(channelsToPart.Remove(channelValues[0])) continue;
			channelsToJoin.Add(channelValues[0]);
			channelsToJoinKeys.Add(channelValues.Length > 1 ? channelValues[1] : null);
		}

		foreach(IrcChannel channel in oldConfig.IrcClient.Channels){
			if(channelsToPart.Contains(channel.Name)){
				await channel.PartAsync("Rehashing");
			}
		}

		for(int index = 0; index < channelsToJoin.Count; index++){
			await oldConfig.IrcClient.Channels.JoinAsync(channelsToJoin[index], channelsToJoinKeys[index]); // Function ignores null keys
		}
	}

	protected override async void ExitHandler(object sender, EventArgs args){await IrcClient.QuitAsync("Shutting down");}

	private static void OnError(object sender, ErrorEventArgs e){Logger.Error(e.Error, e.Error.Message);}

	private enum CtcpCommands{ PING, FINGER, VERSION, USERINFO, CLIENTINFO, SOURCE, TIME, PAGE, AVATAR }
}
