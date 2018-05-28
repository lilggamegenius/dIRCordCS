﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Common.Logging;
using dIRCordCS.Bridge;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using IrcDotNet;
using Newtonsoft.Json;
using NLog.Fluent;
using Configuration = dIRCordCS.Config.Configuration;
using Environment = System.Environment;
using Exception = System.Exception;
using LogLevel = NLog.LogLevel;
using LogManager = Common.Logging.LogManager;
using String = System.String;

namespace dIRCordCS{
	internal class Program{
		public static long CurrentTimeMillis=>DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		public const string ErrorMsg = ". If you see this a lot, add a issue on the Issue tracker https://github.com/lilggamegenuis/dIRCord/issues";
		private const string KvircFlags = "\u00034\u000F";
		private static readonly ILog Logger = LogManager.GetLogger<Program>();
		private const int Attempts = 10;
		private const int ConnectDelay = 15 * 1000;
		private static readonly FileInfo ThisBinary;
		private static readonly DateTime LastModified;
		private static readonly JsonSerializer Serializer = new JsonSerializer();
		private static FileInfo _configFile;
		public static long LastActivity = CurrentTimeMillis; // activity as in people talking
		public static Configuration[] Config;
		public static Dictionary<DiscordChannel, DiscordUser> LastUserToSpeak = new Dictionary<DiscordChannel, DiscordUser>();
		public static IrcClientManager Manager = new IrcClientManager();
		//private static readonly MultiBotManager Manager = new MultiBotManager();

		static Program(){
			ThisBinary = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
			LastModified = ThisBinary.LastWriteTime;
		}

		public static void InitConfigs(){
			if(Config != null){
				InitConfigs(ref Config);
			}
		}

		public static void InitConfigs(ref Configuration[] configurations){
			for(var index = 0; index < configurations.Length; index++){
				InitConfig(ref configurations[index]);
			}
		}

		public static void InitConfig(ref Configuration configuration){
			configuration.nickname = configuration.nickname ?? "dIRCord";
			configuration.userName = configuration.userName ?? configuration.nickname;
			configuration.realName = configuration.realName ?? configuration.nickname + " " + configuration.userName;
			configuration.port = configuration.port == 0 ? 6667 : configuration.port;
			configuration.floodProtectionDelay =
				configuration.floodProtectionDelay == 0 ? 1000 : configuration.floodProtectionDelay;
		}

		public static int Main(string[] args){
			string configFilePath;
			if(args.Length == 0){
				configFilePath = "config.json";
			}
			else{
				configFilePath = args[0];
			}
			_configFile = new FileInfo(configFilePath);
			Logger.Info("Path = " + _configFile);
			try{
				using(var sr = new StreamReader(_configFile.OpenRead()))
				using(var reader = new JsonTextReader(sr)){
					Config = Serializer.Deserialize<Configuration[]>(reader);
					for(byte i = 0; i < Config.Length; i++){
						Config[i].ircClient = new StandardIrcClient();
						Config[i].ircClient.RawMessageSent += (sender, args2)=>Logger.DebugFormat("IRC Send: {0}", args2.RawContent);
						Config[i].ircClient.RawMessageReceived += (sender, args2)=>Logger.DebugFormat("IRC Received: {0}", args2.RawContent);
						Config[i].ircClient.Error += (sender, args2)=>throw args2.Error;
						Config[i].ircListener = new IrcListener(i);
						var i1 = i;
						new Thread(() => {
							Thread.CurrentThread.IsBackground = true;
							Config[i1].discordSocketClient = new DiscordClient(new DiscordConfiguration{
								Token = Config[i1].discordToken,
								LogLevel = DSharpPlus.LogLevel.Debug
							});
							Config[i1].discordListener = new DiscordListener(i1);
						}).Start();
					}
				}

				bool isExit = false;
				while (!isExit)
				{
					Console.Write ("> ");
					var command = Console.ReadLine();
					switch (command)
					{
					case "exit":
						isExit = true;
						break;
					default:
						if (!string.IsNullOrEmpty(command))
						{
							if (command.StartsWith("/") && command.Length > 1)
							{
								//client.SendRawMessage(command.Substring(1));
							}
							else
							{
								Console.WriteLine("unknown command '{0}'", command);
							}
						}
						break;
					}
				}
				//client.Disconnect();
			}
			catch(Exception e){
				Logger.ErrorFormat("Error starting bot", e);
				throw; // Todo remove when done debugging
			}
			return 0;
		}

		public static void rehash(){
			try{
				using(var sr = new StreamReader(_configFile.OpenRead()))
				using(var reader = new JsonTextReader(sr)){
					Configuration[] configs = Serializer.Deserialize<Configuration[]>(reader);
					if(Config.Length == 0){
						Logger.Error("Config file is empty");
						return;
					}

					for(byte i = 0; i < configs.Length; i++){
						Configuration config = configs[i];
						config.channelMapObj = Config[i].channelMapObj;
						config.ircListener = Config[i].ircListener;
						config.discordListener = Config[i].discordListener;
						config.ircClient = Config[i].ircClient;
						config.discordSocketClient = Config[i].discordSocketClient;
						if(!config.discordToken.Equals(Config[i].discordToken))
							Logger.Info("Discord token change will take affect on next restart");
						if(!config.server.Equals(Config[i].server) ||
						   config.port != Config[i].port ||
						   config.SSL != Config[i].SSL){
							Logger.Info("IRC server changes will take affect on next restart");
							continue;
						}
						config.ircListener.Rehash(ref config, ref Config[i]);
						config.discordListener.Rehash(ref config, ref Config[i]);
					}

					Config = configs;
				}
			}
			catch(Exception e){
				if(e is JsonException /*| e is IllegalStateException*/){
					Logger.Error("Error reading config json", e);
					using(var sr = new StreamWriter(new FileInfo("EmptyConfig.json").OpenWrite()))
					using(var emptyFile = new JsonTextWriter(sr)){
						Configuration[] empty = {new Configuration()};
						InitConfigs(ref empty);
						Serializer.Serialize(emptyFile, empty);
					}
				}
				else{
					Logger.Error("Error", e);
				}
			}
		}
	}
}
