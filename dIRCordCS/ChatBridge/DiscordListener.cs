using System;
using System.Text;
using System.Threading.Tasks;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace dIRCordCS.ChatBridge{
	public class DiscordListener : Listener{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private readonly DiscordClient _client;

		public DiscordListener(byte configId) : base(configId){
			_client = Config.DiscordClient = new DiscordClient(new DiscordConfiguration{
				Token = Config.DiscordToken,
				MinimumLogLevel = LogLevel.Trace, // Log everything so NLog can set the log level
				LoggerFactory = new NLogLoggerFactory()
			});
			_client.MessageCreated += OnNewMessage;
			_client.GuildMemberAdded += OnGuildMemberAdded;
			_client.GuildMemberRemoved += OnGuildMemberRemoved;
			_client.GuildMemberUpdated += OnGuildMemberUpdated;
			_client.Ready += OnClientOnReady;
			_client.ClientErrored += OnClientError;
			_client.SocketErrored += OnSocketError;
			AppDomain.CurrentDomain.ProcessExit += ExitHandler;
			_client.ConnectAsync();
		}
		private async Task OnNewMessage(DiscordClient sender, MessageCreateEventArgs e){
			var builder = new StringBuilder();
			foreach(DiscordAttachment result in e.Message.Attachments){
				if(builder.Length != 0){
					builder.Append(", ");
				}

				builder.Append(result.Url);
			}

			DiscordMember member = null;
			try{
				member = await e.Guild.GetMemberAsync(e.Author.Id);
			}
			catch(NotFoundException){
				Logger.Warn($"Unable to find Member for <{e.Author.Username}#{e.Author.Discriminator}> ({e.Author.Id}) in [{e.Guild.Name}] ({e.Guild.Id})");
			}

			if(!await Bridge.CommandHandler(this, sender, member, e)){
				Logger.Info("Message from ({0}) #{1} by {2}: {3} {4}",
							e.Guild.Name,
							e.Channel.Name,
							e.Author.GetHostMask(),
							e.Message.Content.SanitizeForIRC(),
							builder);
				await Bridge.SendMessage(e.Message, e.Channel, member, this, ConfigId);
			}
			else{
				Logger.Info("Command from ({0}) #{1} by {2}: {3} {4}",
							e.Guild.Name,
							e.Channel.Name,
							e.Author.GetHostMask(),
							e.Message.Content.SanitizeForIRC(),
							builder);
			}
		}

		private async Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e){
			DiscordGuild guild = e.Guild;
			DiscordMember member = e.Member;
			foreach(DiscordChannel channel in Program.Config.Servers[ConfigId].ChannelMapObj.Values){
				if(guild != channel.Guild){
					continue;
				}

				Bridge.GetChannel(this, channel).SendMessage($"{member.FormatName(ConfigId)} [{member.Id}] has Joined #{channel.Name}");
			}
		}

		private async Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e){
			DiscordGuild guild = e.Guild;
			DiscordMember member = e.Member;
			foreach(DiscordChannel channel in Program.Config.Servers[ConfigId].ChannelMapObj.Values){
				if(guild != channel.Guild){
					continue;
				}

				Bridge.GetChannel(this, channel).SendMessage($"{member.FormatName(ConfigId)} [{member.Id}] has quit #{channel.Name}");
			}
		}

		private async Task OnGuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e){
			DiscordGuild guild = e.Guild;
			DiscordMember member = e.Member;
			foreach(DiscordChannel channel in Program.Config.Servers[ConfigId].ChannelMapObj.Values){
				if(guild != channel.Guild){
					continue;
				}

				Bridge.GetChannel(this, channel)
					  .SendMessage($"*{member.FormatName(ConfigId, e.NicknameBefore).ToItalics()}* Has changed nick to {member.FormatName(ConfigId, e.NicknameAfter)}");
			}
		}
		private async Task OnClientOnReady(DiscordClient sender, ReadyEventArgs readyEventArgs){
			Config.DiscordReady = true;
			await Task.Run(()=>{
				Bridge.FillMap(ConfigId);
			});
		}

		public override void Rehash(Configuration.ServerConfigs newConfig, Configuration.ServerConfigs oldConfig){}

		protected override async void ExitHandler(object sender, EventArgs args){
			await _client.DisconnectAsync();
		}

		private async Task OnClientError(DiscordClient sender, ClientErrorEventArgs e){
			Logger.Error(e.Exception, e.EventName);
			if(e.Exception is AggregateException exception){
				for(int i = 0; i < exception.InnerExceptions.Count; i++){
					Exception innerException = exception.InnerExceptions[i];
					Logger.Error(innerException, $"[{i + 1}] {innerException.Message}: \n{innerException.StackTrace}");
				}
			}
			else{
				Logger.Error(e.Exception, $"{e.Exception.Message}: {e.Exception.StackTrace}");
			}
		}

		private void PrintException(Exception ex){}

		private async Task OnSocketError(DiscordClient sender, SocketErrorEventArgs e)=>Logger.Error(e.Exception, e.Exception.Message);
	}
}
