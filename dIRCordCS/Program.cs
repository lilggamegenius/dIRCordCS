using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dIRCordCS.ChatBridge;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using GistsApi;
using Newtonsoft.Json;
using NLog;

namespace dIRCordCS;

internal static class Program{
	public const string Version = "dIRCord C# v0.1";
	public const string ErrorMsg = ". If you see this a lot, add a issue on the Issue tracker https://github.com/lilggamegenius/dIRCordCS/issues";
	private const string KvircFlags = "\u00034\u000F";
	private const int Attempts = 10;
	private const int ConnectDelay = 15 * 1000;
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	private static readonly FileInfo ThisBinary;
	private static readonly DateTime LastModified;
	private static readonly JsonSerializer Serializer = new();
	private static FileInfo _configFile;
	public static long LastActivity = CurrentTimeMillis; // activity as in people talking
	public static Configuration Config;

	static Program(){
		ThisBinary = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
		LastModified = ThisBinary.LastWriteTime;
	}
	public static long CurrentTimeMillis=>DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

	public static int Main(string[] args){
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		string configFilePath;
		if(args.Length == 0){
			configFilePath = "config.json";
		} else{
			configFilePath = args[0];
		}

		_configFile = new FileInfo(configFilePath);
		Logger.Info("Path = " + _configFile);
		LoadProgram();
		return 0;
	}

	private static void LoadProgram(){
		var exitEvent = new ManualResetEvent(false);
		Console.CancelKeyPress += (_, eventArgs)=>{
			eventArgs.Cancel = true;
			exitEvent.Set();
		};
		try{
			RunProgram();
			exitEvent.WaitOne();
		} catch(JsonException e){
			Logger.Error(e, "Error reading config json: ({Name}) {Message}\n{StackTracee}", e.GetType().Name, e.Message, e.StackTrace);
			var emptyConfigFile = new FileInfo("EmptyConfig.json");
			using var sr = new StreamWriter(emptyConfigFile.OpenWrite());
			using var emptyFile = new JsonTextWriter(sr);
			var empty = new Configuration();
			Serializer.Serialize(emptyFile, empty);
			Logger.Info($"Empty config file saved to {emptyConfigFile.FullName}");
		} catch(Exception e){
			Logger.Error(e, "Error starting bot: ({Name}) {Message}\n{StackTrace}", e.GetType().Name, e.Message, e.StackTrace);
		}
	}

	private static void RunProgram(){
		using var sr = new StreamReader(_configFile.OpenRead());
		using var reader = new JsonTextReader(sr);
		Config = Serializer.Deserialize<Configuration>(reader);
		if(!string.IsNullOrWhiteSpace(Config.GithubGistOAuthToken)){
			Config.GistClient = new GistClient(Config.GithubGistOAuthToken, Config.UserAgent);
		}

		for(byte i = 0; i < Config.Servers.Length; i++){
			Config.Servers[i].MainConfig = Config;
			Config.Servers[i].IrcListener = new IrcListener(i);
			Config.Servers[i].DiscordListener = new DiscordListener(i);
		}
	}

	private static async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e){
		var exception = e.ExceptionObject as Exception;
		if(exception is ResetException resetException){
			Logger.Warn($"Recived {resetException.GetType().Name}: {resetException.Message}");
			await PrepareReset(resetException.Message);
			LoadProgram();
		} else{
			Logger.Fatal(exception, "Unhandled Exception caught: {Message}\n{StackTrace}", exception?.Message, exception?.StackTrace);
		}
	}

	private static async Task PrepareReset(string message){
		List<Task> quitTasks = new();
		foreach(Configuration.ServerConfigs server in Config.Servers){
			server.DiscordClient.Dispose();
			quitTasks.Add(server.IrcClient.QuitAsync(message));
		}

		await Task.WhenAll(quitTasks);
	}

	public static void Rehash(){
		try{
			using var sr = new StreamReader(_configFile.OpenRead());
			using var reader = new JsonTextReader(sr);
			var configs = Serializer.Deserialize<Configuration>(reader);
			if(Config.Servers.Length == 0){
				Logger.Error("Config file is empty");
				return;
			}

			for(byte i = 0; i < configs.Servers.Length; i++){
				Configuration.ServerConfigs config = configs.Servers[i];
				config.ChannelMapObj = Config.Servers[i].ChannelMapObj;
				config.IrcListener = Config.Servers[i].IrcListener;
				config.DiscordListener = Config.Servers[i].DiscordListener;
				config.IrcClient = Config.Servers[i].IrcClient;
				config.DiscordClient = Config.Servers[i].DiscordClient;
				if(!config.DiscordToken.Equals(Config.Servers[i].DiscordToken)){
					Logger.Info("Discord token change will take affect on next restart");
				}

				if(!config.Server.Equals(Config.Servers[i].Server) ||
				   (config.Port != Config.Servers[i].Port)         ||
				   (config.Ssl  != Config.Servers[i].Ssl)){
					Logger.Info("IRC server changes will take affect on next restart");
					continue;
				}

				config.IrcListener.Rehash(config, Config.Servers[i]);
				config.DiscordListener.Rehash(config, Config.Servers[i]);
			}

			Config = configs;
		} catch(JsonException e){
			Logger.Error(e, "Error reading config json: ({Name}) {Message}\n{StackTrace}", e.GetType().Name, e.Message, e.StackTrace);
			using var sr = new StreamWriter(new FileInfo("EmptyConfig.json").OpenWrite());
			using var emptyFile = new JsonTextWriter(sr);
			var empty = new Configuration();
			Serializer.Serialize(emptyFile, empty);
		} catch(Exception e){
			Logger.Error(e, "Error: ({Name}) {Message}\n{StackTrace}", e.GetType().Name, e.Message, e.StackTrace);
		}
	}
}
