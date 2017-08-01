using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dIRCordCS.Utils;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;
using org.pircbotx;
using Configuration = dIRCordCS.Config.Configuration;
using Exception = System.Exception;
using String = System.String;
using Thread = System.Threading.Thread;

namespace dIRCordCS{
	internal class Program{
		public static long CurrentTimeMillis=>DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		const string errorMsg = ". If you see this a lot, add a issue on the Issue tracker https://github.com/lilggamegenuis/dIRCord/issues";
		private const string kvircFlags = "\u00034\u000F";
		private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
		private const int attempts = 10;
		private const int connectDelay = 15 * 1000;
		private static readonly MultiBotManager manager = new MultiBotManager();
		private static readonly FileInfo thisJar;
		private static readonly DateTime lastModified;
		private static readonly JsonSerializer serializer = new JsonSerializer();
		private static FileInfo configFile;
		public static long lastActivity = CurrentTimeMillis; // activity as in people talking
		public static Configuration[] config;
		public static Dictionary<SocketGuildChannel, SocketGuildUser> LastUserToSpeak = new Dictionary<SocketGuildChannel, SocketGuildUser>();

		static Program(){
			thisJar = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
			lastModified = thisJar.LastWriteTime;
		}
		
		public static void Main(string[] args)
			=> new Program().MainAsync(args).GetAwaiter().GetResult();

		public async Task MainAsync(string[] args){
		//LOGGER.setLevel(Level.ALL);
		new Thread(() => {
			try {
				LOGGER.Trace("Starting updater thread");
				while (true) {
					Thread.Sleep(60 * 1000);
					if ((lastActivity + (1000 * 60 * config[0].minutesOfInactivityToUpdate)) < CurrentTimeMillis) {
						LOGGER.Trace("Checking for new build");
						checkForNewBuild();
					}
				}
			} catch (Exception e) {
				LOGGER.Error($"Error in update thread: {e}");
			}
		}).Start();
		string configFilePath;
		if (args.Length == 0) {
			configFilePath = "config.json";
		} else {
			configFilePath = args[0];
		}
		configFile = new FileInfo(configFilePath);
		LOGGER.Info("Path = " + configFile);
		try {using(var sr = new StreamReader(configFile.OpenRead()))
			using(var reader = new JsonTextReader(sr)){
				config = serializer.Deserialize<Configuration[]>(reader);
				if(config.Length == 0){
					LOGGER.Error("Config file is empty");
					Environment.Exit(-1);
				}
				for(byte i = 0; i < config.Length; i++){
					Configuration config = Program.config[i];
					config.channelMapping = new BiDictionary<string, string>();
					org.pircbotx.Configuration ircConfig;
					org.pircbotx.Configuration.Builder configBuilder = new org.pircbotx.Configuration.Builder()
						.setAutoReconnectDelay(connectDelay)
						.setEncoding(Charset.forName("UTF-8"))
						.setAutoReconnect(true)
						.setAutoReconnectAttempts(attempts)
						.setNickservPassword(config.nickservPassword)
						.setName(config.nickname) //Set the nick of the bot.
						.setLogin(config.userName)
						.setAutoSplitMessage(config.autoSplitMessage)
						.setRealName(kvircFlags + config.realName);
					foreach(string channel in config.channelMapping.Values){
						string[] channelValues = channel.Split(null, 1);
						if(channelValues.Length > 1){ configBuilder.addAutoJoinChannel(channelValues[0], channelValues[1]); }
						else{ configBuilder.addAutoJoinChannel(channelValues[0]); }
					}
					if(config.floodProtection){ configBuilder.setMessageDelay(config.floodProtectionDelay); }
					if(config.SSL){ configBuilder.setSocketFactory(new UtilSSLSocketFactory().trustAllCertificates()); }
					config.ircListener = new IrcListener(i);
					config.discordListener = new DiscordListener(i);
					ircConfig = configBuilder.addListener(config.ircListener).buildForServer(config.server, config.port);
					manager.addBot(ircConfig);
					String token = config.discordToken;
					LOGGER.Trace("Calling JDA Builder with token: " + token);
					config.ws = new JDABuilder(AccountType.BOT)
						.setToken(token)
						.setAutoReconnect(true)
						.setEnableShutdownHook(true)
						.addEventListener(config.discordListener)
						.buildBlocking();
					LOGGER.trace("JDA built\n" + config.jda);
				}
				manager.start();
			}
		} catch (JsonException e) {
			using(var sr = new StreamWriter(new FileInfo("EmptyConfig.json").OpenWrite()))
			using(var emptyFile = new JsonTextWriter(sr)){
				LOGGER.Error("Error reading config json", e);
				serializer.Serialize(emptyFile,new []{new Configuration()});
			}
		} catch (Exception e) {
			LOGGER.Error("Error", e);
		}

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		private static void checkForNewBuild(){
			if(Monitor.TryEnter(kvircFlags)){
				FileInfo newJar = new FileInfo(thisJar + ".new");
				LOGGER.Trace("This jar: " + thisJar.Name + " New jar: " + newJar.Name);
				if (!newJar.Exists || thisJar.LastWriteTime == lastModified) {
					LOGGER.Trace("no new build found");
					return;
				}
				LOGGER.Trace("Found build, exiting with code 1");
				manager.stop("Updating bridge");
				foreach(Configuration configuration in config) {
					configuration.discordSocketClient.StopAsync().Start();
				}
				Environment.Exit(1); // tell wrapper that new jar was found
			}
		}
		
		public static void rehash() {
			try {using(var sr = new StreamReader(configFile.OpenRead()))
				using(var reader = new JsonTextReader(sr)){
					config = serializer.Deserialize<Configuration[]>(reader);
					if(config.Length == 0){
						LOGGER.Error("Config file is empty");
						return;
					}
					Configuration[] configs = gson.fromJson(reader, Configuration[].class);
			for (byte i = 0; i < configs.length; i++) {
				Configuration config = configs[i];
				config.channelMapObj = Program.config[i].channelMapObj;
				config.ircListener = Program.config[i].ircListener;
				config.discordListener = Program.config[i].discordListener;
				config.pircBotX = Program.config[i].pircBotX;
				config.jda = Program.config[i].jda;
				if (!config.discordToken.equals(Program.config[i].discordToken))
					LOGGER.info("Discord token change will take affect on next restart");
				if (!config.server.equals(Program.config[i].server) ||
						config.port != Program.config[i].port ||
						config.SSL != Program.config[i].SSL) {
					LOGGER.info("IRC server changes will take affect on next restart change will take affect on next restart");
					continue;
				}
				config.channelMapping = HashBiMap.create(config.channelMapping);
				List<string> channelsToJoin = new List<string>();
				List<string> channelsToJoinKeys = new List<string>();
				List<string> channelsToPart = new List<string>(Program.config[i].channelMapping.values());
				foreach(string channel in config.channelMapping.values()) {
					String[] channelValues = channel.split(" ", 1);
					if (!channelsToPart.remove(channelValues[0])) {
						channelsToJoin.add(channelValues[0]);
						if (channelValues.length > 1) {
							channelsToJoinKeys.add(channelValues[1]);
						} else {
							channelsToJoinKeys.add(null);
						}
					}
				}
				foreach(string channelToPart in channelsToPart) {
					Program.config[i].pircBotX.sendRaw().rawLine("PART " + channelToPart + " :Rehashing");
				}
				for (int index = 0; index < channelsToJoin.Size; index++) {
					if (channelsToJoinKeys.get(index) != null) {
						Program.config[i].pircBotX.send().joinChannel(channelsToJoin.get(index), channelsToJoinKeys.get(index));
					} else {
						Program.config[i].pircBotX.send().joinChannel(channelsToJoin.get(index));
					}
				}
			}
			Program.config = configs;
		} catch (JsonSyntaxException | IllegalStateException e) {
			try (FileWriter emptyFile = new FileWriter(new string("EmptyConfig.json"))) {
				LOGGER.error("Error reading config json", e);
				emptyFile.write(gson.toJson(new Configuration[]{new Configuration()}));
			} catch (Exception e2) {
				LOGGER.error("Error writing empty file", e2);
			}
		} catch (Exception e) {
			LOGGER.error("Error", e);
		}
	}

		public Task Log(LogMessage msg){
			switch (msg.Severity){
			case LogSeverity.Critical: 
				LOGGER.Fatal(msg.Message);
				break;
			case LogSeverity.Error:
				LOGGER.Error(msg.Message);
				break;
			case LogSeverity.Warning:
				LOGGER.Warn(msg.Message);
				break;
			case LogSeverity.Info:
				LOGGER.Info(msg.Message);
				break;
			case LogSeverity.Verbose:
				LOGGER.Trace(msg.Message);
				break;
			case LogSeverity.Debug:
				LOGGER.Debug(msg.Message);
				break;
			}
			return Task.CompletedTask;
		}
	}
}