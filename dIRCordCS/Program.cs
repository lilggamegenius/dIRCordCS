using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Common.Logging;
using dIRCordCS.Listeners;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using IrcDotNet;
using Newtonsoft.Json;
using Configuration = dIRCordCS.Config.Configuration;
using Environment = System.Environment;
using Exception = System.Exception;
using LogLevel = NLog.LogLevel;
using LogManager = Common.Logging.LogManager;
using String = System.String;
using Thread = System.Threading.Thread;

namespace dIRCordCS{
	internal class Program{
		public static long CurrentTimeMillis=>DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		public const string ErrorMsg = ". If you see this a lot, add a issue on the Issue tracker https://github.com/lilggamegenuis/dIRCord/issues";
		private const string KvircFlags = "\u00034\u000F";
		private static readonly ILog Logger;
		private const int Attempts = 10;
		private const int ConnectDelay = 15 * 1000;
		private static readonly FileInfo ThisJar;
		private static readonly DateTime LastModified;
		private static readonly JsonSerializer Serializer = new JsonSerializer();
		private static FileInfo _configFile;
		public static long LastActivity = CurrentTimeMillis; // activity as in people talking
		public static Configuration[] Config;
		public static Dictionary<DiscordChannel, DiscordUser> LastUserToSpeak = new Dictionary<DiscordChannel, DiscordUser>();
		public static IrcClientManager Manager = new IrcClientManager();
		//private static readonly MultiBotManager Manager = new MultiBotManager();

		static Program(){
			ThisJar = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
			LastModified = ThisJar.LastWriteTime;
			Logger = LogManager.GetLogger<Program>();
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
			LogLevel(NLog.LogLevel.Trace);
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
				if(!_configFile.Exists){
					throw new FileNotFoundException(_configFile.FullName);
				}

				using(var sr = new StreamReader(_configFile.OpenRead()))
				using(var reader = new JsonTextReader(sr)){
					Config = Serializer.Deserialize<Configuration[]>(reader);
					InitConfigs();
					if(Config.Length == 0){
						Logger.Error("Config file is empty");
						return 2;
					}

					for(byte i = 0; i < Config.Length; i++){
						ref Configuration config = ref Config[i];
						config.channelMapping = new BiDictionary<string, string>();
						var ircConfig = new IrcUserRegistrationInfo(){
							NickName = config.nickname,
							UserName = config.userName,
							RealName = KvircFlags + config.realName,

						};
						var client = new StandardIrcClient();
						//org.pircbotx.Configuration ircConfig;
						/*org.pircbotx.Configuration.Builder configBuilder = new org.pircbotx.Configuration.Builder()
						                                                   .setAutoReconnectDelay(ConnectDelay)
						                                                   .setEncoding(Charset.forName("UTF-8"))
						                                                   .setAutoReconnect(true)
						                                                   .setAutoReconnectAttempts(Attempts)
						                                                   .setName(config.nickname) //Set the nick of the bot.
						                                                   .setLogin(config.userName)
						                                                   .setAutoSplitMessage(config.autoSplitMessage)
						                                                   .setRealName(KvircFlags + config.realName);*/
						/*if(string.IsNullOrEmpty(config.nickservPassword)){
							configBuilder.setNickservPassword(config.nickservPassword);
						}*/
						foreach(string channel in config.channelMapping.Values){
							string[] channelValues = channel.Split(null, 1);
							if(channelValues.Length > 1){
								configBuilder.addAutoJoinChannel(channelValues[0], channelValues[1]);
							}
							else{
								configBuilder.addAutoJoinChannel(channelValues[0]);
							}
						}

						if(config.floodProtection){
							configBuilder.setMessageDelay(config.floodProtectionDelay);
						}

						if(config.SSL){
							configBuilder.setSocketFactory(new UtilSSLSocketFactory().trustAllCertificates());
						}

						config.ircListener = new IrcListener(i);
						ircConfig = configBuilder.addListener(config.ircListener).buildForServer(config.server, config.port);
						Manager.Add(ircConfig);
						String token = config.discordToken;
						foreach(Configuration conf in Config){
							if(conf.discordToken == token){
								config.discordSocketClient = conf.discordSocketClient;
							}
						}

						if(config.discordSocketClient == null){
							Logger.Trace("Calling discordSocketClient Builder with token: " + token);
							var cfg = new DiscordConfiguration{
								Token = token,
								TokenType = TokenType.Bot,
								AutoReconnect = true,
								LogLevel = DSharpPlus.LogLevel.Debug,
								UseInternalLogHandler = false
							};
							config.discordSocketClient = new DiscordClient(cfg);
							Logger.Trace("DSharpPlus built\n" + config.discordSocketClient);
							config.discordSocketClient.ConnectAsync();
						} else{
							Logger.Trace("Using already existing Discord connection");
						}
						config.discordListener = new DiscordListener(i);
					}

					Manager.Start();

					new Thread(()=>{
						try{
							Logger.Trace("Starting updater thread");
							while(true){
								Thread.Sleep(60 * 1000);
								if((LastActivity + (1000 * 60 * Config[0].minutesOfInactivityToUpdate)) < CurrentTimeMillis){
									Logger.Trace("Checking for new build");
									checkForNewBuild();
								}
							}
						}
						catch(Exception e){
							Logger.Error($"Error in update thread: {e}");
						}
					}).Start();
				}
			}
			catch(Exception e){
				if(e is JsonException ||
				   e is FileNotFoundException){
					using(var sr = new StreamWriter(new FileInfo("EmptyConfig.json").OpenWrite()))
					using(var emptyFile = new JsonTextWriter(sr)){
						Logger.Error("Error reading config json", e);
						emptyFile.Formatting = Formatting.Indented;
						Configuration[] empty = {new Configuration()};
						InitConfigs(ref empty);
						Serializer.Serialize(emptyFile, empty);
					}
					return 1;
				}
				Logger.Error($"Error: {e}");
			}
			return 0;
		}

		private static void checkForNewBuild(){
			if(Monitor.TryEnter(KvircFlags)){
				FileInfo newJar = new FileInfo(ThisJar + ".new");
				Logger.Trace("This jar: " + ThisJar.Name + " New jar: " + newJar.Name);
				if(!newJar.Exists ||
				   ThisJar.LastWriteTime == LastModified){
					Logger.Trace("no new build found");
					return;
				}

				Logger.Trace("Found build, exiting with code 1");
				Manager.Stop("Updating bridge");
				foreach(Configuration configuration in Config){
					configuration.discordSocketClient.DisconnectAsync().Start();
				}

				Environment.Exit(1); // tell wrapper that new jar was found
			}
		}

		public static void rehash(){
			try{
				using(var sr = new StreamReader(_configFile.OpenRead()))
				using(var reader = new JsonTextReader(sr)){
					Config = Serializer.Deserialize<Configuration[]>(reader);
					if(Config.Length == 0){
						Logger.Error("Config file is empty");
						return;
					}

					Configuration[] configs = Serializer.Deserialize<Configuration[]>(reader);
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
							Logger.Info("IRC server changes will take affect on next restart change will take affect on next restart");
							continue;
						}

						//config.channelMapping = HashBiMap.create(config.channelMapping);
						List<string> channelsToJoin = new List<string>();
						List<string> channelsToJoinKeys = new List<string>();
						List<string> channelsToPart = new List<string>(Config[i].channelMapping.Values);
						foreach(string channel in config.channelMapping.Values){
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
						Config[i].ircClient.Channels.Leave(channelsToPart, "Rehashing");

						for(int index = 0; index < channelsToJoin.Count; index++){
							if(channelsToJoinKeys[index] != null){
								Config[i].ircClient.Channels.Join(new Tuple<string, string>(channelsToJoin[index], channelsToJoinKeys[index]));
							}
							else{
								Config[i].ircClient.Channels.Join(channelsToJoin[index]);
							}
						}
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

		public static void LogLevel(LogLevel level){
			/*LoggingConfiguration configuration = LogManager.Configuration;
			if(configuration == null){
				configuration = new LoggingConfiguration();
				LogManager.Configuration = configuration;
			}
			foreach(var rule in configuration.LoggingRules)
			{
				rule.EnableLoggingForLevel(level);
			}

			//Call to update existing Loggers created with GetLogger() or
			//GetCurrentClassLogger()
			LogManager.ReconfigExistingLoggers();*/
		}
	}
}
