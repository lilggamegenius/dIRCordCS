namespace dIRCordCS.ChatBridge{
	using System;
	using System.Text;
	using System.Threading.Tasks;
	using Common.Logging;
	using dIRCordCS.Config;
	using dIRCordCS.Utils;
	using DSharpPlus;
	using DSharpPlus.Entities;
	using DSharpPlus.EventArgs;
	using LogLevel = DSharpPlus.LogLevel;

	public class DiscordListener : Listener{
		private static readonly ILog Logger = LogManager.GetLogger<DiscordListener>();
		private readonly DiscordClient _client;

		public DiscordListener(byte configId) : base(configId){
			_client = Config.DiscordSocketClient = new DiscordClient(new DiscordConfiguration{
				Token = Config.DiscordToken,
				LogLevel = LogLevel.Debug
			});
			_client.DebugLogger.LogMessageReceived += (sender, args)=>{
				switch(args.Level){
					case LogLevel.Debug:
						Logger.Debug(args.Message);
						break;
					case LogLevel.Info:
						Logger.Info(args.Message);
						break;
					case LogLevel.Warning:
						Logger.Warn(args.Message);
						break;
					case LogLevel.Error:
						Logger.Error(args.Message);
						break;
					case LogLevel.Critical:
						Logger.Fatal(args.Message);
						break;
					default: throw new ArgumentOutOfRangeException();
				}
			};
			_client.MessageCreated += OnNewMessage;
			_client.Ready += OnClientOnReady;
			_client.ClientErrored += OnClientError;
			_client.SocketErrored += OnSocketError;
			AppDomain.CurrentDomain.ProcessExit += ExitHandler;
			_client.ConnectAsync();
		}
		private async Task OnNewMessage(MessageCreateEventArgs e){
			var builder = new StringBuilder();
			foreach(DiscordAttachment result in e.Message.Attachments){
				if(builder.Length != 0){
					builder.Append(", ");
				}

				builder.Append(result.Url);
			}

			Task<DiscordMember> member = e.Guild.GetMemberAsync(e.Author.Id);
			if(!await Bridge.CommandHandler(this, await member, e)){
				Logger.InfoFormat("Message from ({0}) #{1} by {2}: {3} {4}",
								  e.Guild.Name,
								  e.Channel.Name,
								  e.Author.GetHostMask(),
								  e.Message.Content,
								  builder);
				await Bridge.SendMessage(e.Message.Content, e.Channel, await member, this, ConfigId);
			}
			else{
				Logger.InfoFormat("Command from ({0}) #{1} by {2}: {3} {4}",
								  e.Guild.Name,
								  e.Channel.Name,
								  e.Author.GetHostMask(),
								  e.Message.Content,
								  builder);
			}
		}

		private async Task OnClientOnReady(ReadyEventArgs args){
			Config.DiscordReady = true;
			await Task.Run(()=>{
				Bridge.FillMap(ConfigId);
			});
		}

		public override void Rehash(Configuration.ServerConfigs newConfig, Configuration.ServerConfigs oldConfig){}

		protected override async void ExitHandler(object sender, EventArgs args){
			await _client.DisconnectAsync();
		}

		private async Task OnClientError(ClientErrorEventArgs e){
			Logger.Error(e.EventName, e.Exception);
			if(e.Exception is AggregateException exception){
				for(int i = 0; i < exception.InnerExceptions.Count; i++){
					Exception innerException = exception.InnerExceptions[i];
					Logger.Error($"[{i + 1}] {innerException.Message}: \n{innerException.StackTrace}", innerException);
				}
			}
		}

		private async Task OnSocketError(SocketErrorEventArgs e)=>Logger.Error(e.Exception.Message, e.Exception);
	}
}
