﻿using System;
using System.Threading.Tasks;
using Common.Logging;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.EventArgs;
using LogLevel = DSharpPlus.LogLevel;

namespace dIRCordCS.ChatBridge{
	public class DiscordListener : Listener{
		private static readonly ILog Logger = LogManager.GetLogger<DiscordListener>();
		private DiscordClient client;

		public DiscordListener(byte configId) : base(configId){
			client = Config.DiscordSocketClient = new DiscordClient(new DiscordConfiguration{
				Token = Config.DiscordToken,
				LogLevel = LogLevel.Debug
			});
			client.DebugLogger.LogMessageReceived += (sender, args)=>{
				switch(args.Level){
				case LogLevel.Debug: Logger.Debug(args.Message); break;
				case LogLevel.Info: Logger.Info(args.Message); break;
				case LogLevel.Warning: Logger.Warn(args.Message); break;
				case LogLevel.Error: Logger.Error(args.Message); break;
				case LogLevel.Critical: Logger.Fatal(args.Message); break;
				default: throw new ArgumentOutOfRangeException();
				}
			};
			client.MessageCreated += OnNewMessage;
			client.Ready += onClientOnReady;
			client.ConnectAsync();
			AppDomain.CurrentDomain.ProcessExit += ExitHandler;
		}
		private async Task OnNewMessage(MessageCreateEventArgs e){
			await Task.Run(()=>{
				if(!Bridge.CommandHandler(this, e)){
					Logger.InfoFormat("Message from ({0}) #{1} by {2}: {3}",
					                  e.Guild.Name,
					                  e.Channel.Name,
					                  e.Author.GetHostMask(),
					                  e.Message.Content);
				}
				else{
					Logger.InfoFormat("Command from ({0}) #{1} by {2}: {3}",
					                  e.Guild.Name,
					                  e.Channel.Name,
					                  e.Author.GetHostMask(),
					                  e.Message.Content);
				}
			});
		}

		private async Task onClientOnReady(ReadyEventArgs args){
			Config.DiscordReady = true;
			await Task.Run(()=>{
				Bridge.FillMap(ConfigID);
			});
		}

		public override void Rehash(ref Configuration newConfig, ref Configuration oldConfig){}

		protected override async void ExitHandler(object sender, EventArgs args){
			await client.DisconnectAsync();
		}
	}
}