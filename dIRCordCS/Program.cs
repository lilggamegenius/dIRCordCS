using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using dIRCordCS.ChatBridge;
using dIRCordCS.Config;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace dIRCordCS{
	internal class Program{
		public const string Version = "dIRCord C# v0.1";
		public const string ErrorMsg = ". If you see this a lot, add a issue on the Issue tracker https://github.com/lilggamegenius/dIRCordCS/issues";
		private const string KvircFlags = "\u00034\u000F";
		private const int Attempts = 10;
		private const int ConnectDelay = 15 * 1000;
		private static readonly ILog Logger = LogManager.GetLogger<Program>();
		private static readonly FileInfo ThisBinary;
		private static readonly DateTime LastModified;
		private static readonly JsonSerializer Serializer = new JsonSerializer();
		private static FileInfo _configFile;
		public static long LastActivity = CurrentTimeMillis; // activity as in people talking
		public static Configuration[] Config;
		public static Dictionary<DiscordChannel, DiscordUser> LastUserToSpeak = new Dictionary<DiscordChannel, DiscordUser>();
		//public static IrcClientManager Manager = new IrcClientManager();

		static Program(){
			ThisBinary = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
			LastModified = ThisBinary.LastWriteTime;
		}
		public static long CurrentTimeMillis=>DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

		public static void InitConfigs(){
			if(Config != null){ InitConfigs(ref Config); }
		}

		public static void InitConfigs(ref Configuration[] configurations){
			for(int index = 0; index < configurations.Length; index++){ InitConfig(ref configurations[index]); }
		}

		public static void InitConfig(ref Configuration configuration){
			configuration.Nickname = configuration.Nickname ?? "dIRCord";
			configuration.UserName = configuration.UserName ?? configuration.Nickname;
			configuration.RealName = configuration.RealName ?? (configuration.Nickname + " " + configuration.UserName);
			configuration.Port = configuration.Port == 0 ? 6667 : configuration.Port;
			configuration.FloodProtectionDelay =
				configuration.FloodProtectionDelay == 0 ? 1000 : configuration.FloodProtectionDelay;
		}

		public static int Main(string[] args){
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			string configFilePath;
			if(args.Length == 0){ configFilePath = "config.json"; } else{ configFilePath = args[0]; }

			_configFile = new FileInfo(configFilePath);
			Logger.Info("Path = " + _configFile);
			try{
				using(StreamReader sr = new StreamReader(_configFile.OpenRead()))
				using(JsonTextReader reader = new JsonTextReader(sr)){
					Config = Serializer.Deserialize<Configuration[]>(reader);
					for(byte i = 0; i < Config.Length; i++){
						Config[i].IrcListener = new IrcListener(i);
						Config[i].DiscordListener = new DiscordListener(i);
					}
				}

				bool isExit = false;
				while(!isExit){
					Console.Write("> ");
					string command = Console.ReadLine();
					isExit = true;
				}

				//client.Disconnect();
			} catch(Exception e){ Logger.ErrorFormat("Error starting bot: {0}", e); }

			return 0;
		}
		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e){
			Exception exception = e.ExceptionObject as Exception;
			Logger.Fatal($"Unhandled Exception caught: {exception?.Message}\n{exception?.StackTrace}", exception);
		}

		public static void Rehash(){
			try{
				using(StreamReader sr = new StreamReader(_configFile.OpenRead()))
				using(JsonTextReader reader = new JsonTextReader(sr)){
					Configuration[] configs = Serializer.Deserialize<Configuration[]>(reader);
					if(Config.Length == 0){
						Logger.Error("Config file is empty");
						return;
					}

					for(byte i = 0; i < configs.Length; i++){
						Configuration config = configs[i];
						config.ChannelMapObj = Config[i].ChannelMapObj;
						config.IrcListener = Config[i].IrcListener;
						config.DiscordListener = Config[i].DiscordListener;
						config.IrcClient = Config[i].IrcClient;
						config.DiscordSocketClient = Config[i].DiscordSocketClient;
						if(!config.DiscordToken.Equals(Config[i].DiscordToken)){ Logger.Info("Discord token change will take affect on next restart"); }

						if(!config.Server.Equals(Config[i].Server) ||
						   (config.Port != Config[i].Port)         ||
						   (config.Ssl  != Config[i].Ssl)){
							Logger.Info("IRC server changes will take affect on next restart");
							continue;
						}

						config.IrcListener.Rehash(ref config, ref Config[i]);
						config.DiscordListener.Rehash(ref config, ref Config[i]);
					}

					Config = configs;
				}
			} catch(Exception e){
				if(e is JsonException /*| e is IllegalStateException*/){
					Logger.Error("Error reading config json", e);
					using(StreamWriter sr = new StreamWriter(new FileInfo("EmptyConfig.json").OpenWrite()))
					using(JsonTextWriter emptyFile = new JsonTextWriter(sr)){
						Configuration[] empty = {new Configuration()};
						InitConfigs(ref empty);
						Serializer.Serialize(emptyFile, empty);
					}
				} else{ Logger.Error("Error", e); }
			}
		}
	}
}
