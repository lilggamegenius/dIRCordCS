using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;
using dIRCordCS.Config;
using dIRCordCS.Utils;
using DSharpPlus.Entities;
using IrcDotNet.Target.Channel;
using IrcDotNet.Target.User;
using Exception = System.Exception;
using Configuration = dIRCordCS.Config.Configuration;
using static dIRCordCS.Bridge;

namespace dIRCordCS.Listeners{
	public class IrcListener{
		private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
		private readonly byte configID;
		public volatile bool ready;

		public IrcListener(byte configID){
			this.configID = configID;
		}

		public static string getUserSymbol(IrcChannelUser user){
			return getUserLevel(user.Modes).getSymbol().ToString();
		}

		private static string formatName(IrcUser user, bool useHostmask = false){
			string ret = user.NickName;
			if(useHostmask){
				ret = user.HostMask;
			}

			if(user.IsOperator){
				return "__" + ret + "__";
			}

			return ret;
		}

		private bool handleCommand(MessageEvent @event){
			string[] message = LilGUtil.splitMessage(@event.getMessage());
			if(message.Length > 0){
				if(message[0].startsWith(@event.getBot().getNick())){
					if(getDiscordChannel(@event) != null){
						Bridge.handleCommand(message, @event, configID, true);
						return true;
					}
				}
			}

			return false;
		}

		public override async void onEvent(Event @event){
			base.onEvent(@event);
			await fillChannelMap();
		}

		public override async void onConnect(ConnectEvent @event){
			await Task.Run(()=>{
				config().ircClient = @event.getBot();
				foreach(string str in config().autoSendCommands){
					@event.getBot().sendRaw().rawLine(str);
				}

				ready = true;
			});
		}

		public override async void onMessage(MessageEvent @event){
			await Task.Run(()=>{
				try{
					config().ircClient = @event.getBot();
					string message = @event.getMessage();
					DiscordChannel channel = getDiscordChannel(@event);
					for(int tries = 0; channel == null; tries++){
						if(tries > 10){
							@event.respond("Failed sending message to discord" + Program.ErrorMsg);
							return;
						}

						channel = getDiscordChannel(@event);
					}

					IRCChannelConfiguration configuration = channelConfig(@event);
					if(message.StartsWithAny(configuration.getCommmandCharacters())){
						DiscordChannel finalChannel = channel;
						new Action(async ()=>{
							await channel
								.SendMessageAsync($"_Command Sent by_ `{getUserSymbol(@event)}{formatName(@event.getUser(), true)}`");
							await finalChannel.SendMessageAsync(formatString(finalChannel, message));
						}).Invoke();
					}
					else{
						if(!handleCommand(@event)){
							channel.SendMessageAsync($"**<{getUserSymbol(@event)}{formatName(@event.getUser())}>** {formatString(channel, message)}");
						}
					}
				}
				catch(Exception e){
					Logger.Error("Error in IrcListener", e);
				}

				Program.LastActivity = Program.CurrentTimeMillis;
			});
		}

		public override async void onNotice(NoticeEvent @event){
			await Task.Run(()=>{
				string message = @event.getMessage();
				//noinspection ConstantConditions
				if(@event.getUser() == null){
					return;
				}

				if(message.contains("\u0001AVATAR")){
					@event.getUser().send().notice("\u0001AVATAR " + config().discordSocketClient.CurrentUser.AvatarUrl + "\u0001");
				}
			});
		}

		public override async void onJoin(JoinEvent @event){
			IRCChannelConfiguration configuration = channelConfig(@event.getChannel().getName());
			getSpamList(@event.getChannel());
			if(configuration.joins)
				await getDiscordChannel(@event).SendMessageAsync($"**\\*{formatName(@event.getUser())}\\*** _Joined_");
		}

		public override async void onPart(PartEvent @event){
			IRCChannelConfiguration configuration = channelConfig(@event.getChannel().getName());
			if(configuration.parts)
				await getDiscordChannel(@event)
					.SendMessageAsync($"**\\*{formatName(@event.getUser())}\\*** _Left_ Reason: {@event.getReason()}");
		}

		public override async void onKick(KickEvent @event){
			await getDiscordChannel(@event)
				.SendMessageAsync($"**\\*{formatName(@event.getUser())}\\*** Kicked _{formatName(@event.getRecipient())}_: {@event.getReason()}");
		}

		public override async void onAction(ActionEvent @event){
			await getDiscordChannel(@event).SendMessageAsync($"**\\*{formatName(@event.getUser())}\\*** _{@event.getMessage()}_");
		}

		public override async void onMode(ModeEvent @event){
			if(@event.getMode().contains("g")){
				getSpamList(@event.getChannel());
			}

			await getDiscordChannel(@event)
				.SendMessageAsync($"**\\*{formatName(@event.getUser())}\\*** _{"Set mode " + @event.getMode()}_");
		}

		public override async void onQuit(QuitEvent @event){
			//LOGGER.setLevel(Level.ALL);
			foreach(Channel channel in @event.getUser().getChannels()){
				IRCChannelConfiguration configuration = channelConfig(channel.getName());
				if(!configuration.quits)
					continue;
				DiscordChannel textChannel = config().channelMapObj[channel];
				await textChannel.SendMessageAsync(
				                             string.Format("**\\*{0}\\*** _has quit: {1}_",
				                                           formatName(@event.getUser()),
				                                           @event.getReason()
				                                                 .replace("http://www.mibbit.com", "<http://www.mibbit.com>")
				                                                 .replace("http://www.androirc.com/", "<http://www.androirc.com/>")
				                                          )
				                            );
			}
		}

		public override async void onTopic(TopicEvent @event){
			DiscordChannel channel = getDiscordChannel(@event);
			if(channel == null)
				return; //only possible if IRC-OP sajoins bot to another channel
			DateTime time = new DateTime(@event.getDate() * (@event.isChanged() ? 1 : 1000));
			string formattedTime = time.ToString("EEE, d MMM yyyy h:mm:ss a Z");
			if(@event.isChanged()){
				await channel.SendMessageAsync($"{@event.getUser().getHostmask()} has changed topic to: `{@event.getTopic()}` at {formattedTime}");
			}
			else{
				await channel.SendMessageAsync($"Current Topic: `{@event.getTopic()}` set by {@event.getUser().getHostmask()} at {formattedTime}");
			}
		}

		public override async void onNickChange(NickChangeEvent @event){
			//noinspection ConstantConditions
			foreach(Channel channel in @event.getUser().getChannels()){
				DiscordChannel textChannel = config().channelMapObj[channel];
				if(textChannel == null){
					continue;
				}

				string oldNick, newNick;
				oldNick = @event.getOldNick();
				newNick = @event.getNewNick();
				if(@event.getUser().isIrcop()){
					oldNick = "__" + oldNick + "__";
					newNick = "__" + newNick + "__";
				}

				await textChannel.SendMessageAsync($"**\\*{oldNick}\\*** is now known as _%{newNick}_");
			}
		}

		private DiscordChannel getDiscordChannel(GenericChannelEvent @event){
			return config().channelMapObj[@event.getChannel()];
		}

		public async Task fillChannelMap(){
			await Task.Run(()=>{
				if(config().ircClient == null ||
				   config().ircClient.getUserBot().getChannels().size() == 0 ||
				   !ready){
					return;
				}

				if(config().discordSocketClient == null){
					Logger.Warn("JDA is null");
					return;
				}

				Dictionary<string, string> ircDiscordChanMap = config().channelMapping.Inverse();
				foreach(Channel channel in config().ircClient.getUserBot().getChannels()){
					foreach(DiscordGuild guild in config().discordSocketClient.Guilds.Values){
						foreach(DiscordChannel textChannel in guild.Channels){
							if(ircDiscordChanMap[channel.getName()].Equals("#" + textChannel.Name)){
								config().channelMapObj[textChannel] = channel;
							}
						}
					}
				}

				Logger.Info("Filled channel map");
			});
		}

		public async void getSpamList(Channel channel){
			await Task.Run(()=>{
				WaitForQueue queue = new WaitForQueue(channel.getBot());
				//Infinite loop since we might recieve messages that aren't WaitTest's.
				channel.send().setMode("+g");
				try{
					List<string> spamFilterList = config().channelOptions.IRC[channel.getName()].spamFilterList;
					spamFilterList.Clear();
					while(true){
						//Use the waitFor() method to wait for a ServerResponseEvent.
						//This will block (wait) until a ServerResponseEvent comes in, ignoring
						//everything else
						ServerResponseEvent currentEvent = (ServerResponseEvent)queue.waitFor(typeof(ServerResponseEvent));
						//Check if this message is the "ping" command
						if(currentEvent.getCode() == 941){
							spamFilterList.Add((string)currentEvent.getParsedResponse().get(2));
						}
						else if(currentEvent.getCode() == 940){
							Logger.Trace("End of channel spam Filter list");
							queue.close();
							//Very important that we end the infinite loop or else the test
							//will continue forever!
							return;
						}
					}
				}
				catch(InterruptedException e){
					Logger.Warn("Getting spam filter list interrupted", e);
				}
			});
		}

		private ref Configuration config(){
			return ref Program.Config[configID];
		}

		private IRCChannelConfiguration channelConfig(MessageEvent @event){
			return channelConfig(@event.getChannel().getName());
		}

		private IRCChannelConfiguration channelConfig(string channel){
			return config().channelOptions.IRC[channel];
		}
	}
}
