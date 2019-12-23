namespace dIRCordCS.Utils{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using ChatSharp;
	using Common.Logging;
	using dIRCordCS.ChatBridge;
	using DSharpPlus.Entities;

	public static class IrcUtils{
		public const char CtcpChar = '\u0001';
		public const char ColorChar = '\u0003';
		public const char BoldChar = '\u0002';
		public const char ItalicsChar = '\u001D';
		public const char UnderlineChar = '\u001F';
		public const char ReverseChar = '\u0016';
		public const char BellChar = '\u0007';
		public const char SymbolForBellChar = '␇';
		public const char NewLineChar = '\n';
		public const char SymbolForNewLineChar = '␤';
		public const char CharageReturnChar = '\r';
		public const char SymbolForCharageReturnChar = '␍';

		private const string EscapePrefix = "@!";
		private static readonly ILog Logger = LogManager.GetLogger(typeof(IrcUtils));

		public static Dictionary<DiscordChannel, DropOutStack<DiscordUser>> LastUserToSpeak = new Dictionary<DiscordChannel, DropOutStack<DiscordUser>>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToColor(this string str, byte color)=>color >= 16 ? $"{ColorChar}{str}" : $"{ColorChar}{color:00}{str}{ColorChar}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToBold(this string str)=>$"{BoldChar}{str}{BoldChar}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToItalics(this string str)=>$"{ItalicsChar}{str}{ItalicsChar}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToUnderline(this string str)=>$"{UnderlineChar}{str}{UnderlineChar}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToCtcp(this string str)=>$"{CtcpChar}{str}{CtcpChar}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsCtcp(this string str)=>str.StartsWith(CtcpChar.ToString()) && str.EndsWith(CtcpChar.ToString());

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SanitizeForIRC(this string str)=>
			str.Replace(BellChar, SymbolForBellChar)
			   .Replace(NewLineChar, SymbolForNewLineChar)
			   .Replace(CharageReturnChar, SymbolForCharageReturnChar)
			   .Replace("\0", string.Empty);

		public static char GetSymbol(this char mode){
			return mode switch{
				'q'=>'~',
				'a'=>'&',
				'o'=>'@',
				'h'=>'%',
				'v'=>'+',
				var _=>'\0'
			};
		}

		public static bool Contains(this string str, char ch){
			if(str == null){
				return false;
			}

			foreach(char c in str){
				if(c == ch){
					return true;
				}
			}

			return false;
		}

		public static char GetUserLevel(string levels){
			if(levels.Contains('q')){
				return 'q';
			}

			if(levels.Contains('a')){
				return 'a';
			}

			if(levels.Contains('o')){
				return 'o';
			}

			if(levels.Contains('h')){
				return 'h';
			}

			if(levels.Contains('v')){
				return 'v';
			}

			return '\0';
		}

		public static string GetUserSymbol(IrcUser user)=>GetUserLevel(user.Mode).GetSymbol().ToString();

		public static string FormatName(this IrcUser user, byte configId, bool useHostmask = false){
			string ret = user.Nick;
			if(useHostmask){
				ret = user.Hostmask;
			}

			if(user.Mode.Contains('o')){
				ret = ret.FormatUnderline();
			}

			if(user.Match(Program.Config.Servers[configId].IRCBotOwnerHostmask)){
				ret = ret.FormatBold();
			}

			return ret;
		}

		public static async Task<string> ConvertFormatting(string strToFormat, DiscordChannel channel){
			int reverseCount = strToFormat.CountMatches(ReverseChar);
			int underlineCount = strToFormat.CountMatches(UnderlineChar);
			int italicsCount = strToFormat.CountMatches(ItalicsChar);
			int boldCount = strToFormat.CountMatches(BoldChar);
			if(reverseCount != 0){
				strToFormat = strToFormat.Replace(ReverseChar, '`');
				if((reverseCount % 2) != 0){
					strToFormat += '`';
				}
			}

			if(underlineCount != 0){
				strToFormat = strToFormat.Replace(UnderlineChar + "", "__");
				if((underlineCount % 2) != 0){
					strToFormat += "__";
				}
			}

			if(italicsCount != 0){
				strToFormat = strToFormat.Replace(ItalicsChar, '_');
				if((italicsCount % 2) != 0){
					strToFormat += "_";
				}
			}

			if(boldCount != 0){
				strToFormat = strToFormat.Replace(BoldChar + "", "**");
				if((boldCount % 2) != 0){
					strToFormat += "**";
				}
			}

			if(strToFormat.Contains('@')){
				strToFormat = strToFormat.Replace("@everyone", "`@everyone`");
				strToFormat = strToFormat.Replace("@here", "`@here`");
				if(strToFormat.Contains(EscapePrefix)){
					string[] messageCmd = strToFormat.SplitMessage(removeQuotes: false);
					for(int i = 0; i < messageCmd.Length; i++){
						if(!messageCmd[i].StartsWith(EscapePrefix)){
							continue;
						}

						messageCmd[i] = messageCmd[i].Substring(EscapePrefix.Length);
						switch(messageCmd[i]){
							case "last":
								messageCmd[i] = LastUserToSpeak[channel].Peek().Mention;
								break;
						}
					}

					strToFormat = LilGUtil.ArgJoiner(messageCmd);
				}

				//String strLower = strToFormat.toLowerCase();
				//boolean usesNick;
				string[] message = strToFormat.Split(' ');
				double score;
				foreach(string aMessage in message){
					if(aMessage[0] != '@'){
						continue;
					}

					(DiscordMember, double) result = await Bridge.SearchForDiscordUser(aMessage.Substring(1), channel);
					score = result.Item2;
					Logger.Debug($"Found Name {result.Item1.GetHostMask()} from {aMessage} with score {score}");
					if(score > 1){
						Logger.Debug("Ignoring mention");
						continue;
					}

					strToFormat = strToFormat.Replace(aMessage, result.Item1.Mention);
				}
			}

			if(strToFormat.Contains(ColorChar)){
				Regex first = new Regex($"{ColorChar}[0-9]{{1,2}},[0-9]{{1,2}}"), second = new Regex($"{ColorChar}[0-9]{{1,2}}");
				strToFormat = second.Replace(first.Replace(strToFormat, ""), "");
				strToFormat = strToFormat.Replace(ColorChar + "", "");
			}

			return strToFormat;
		}

		public static bool MatchHostMask(this string hostmask, string pattern){
			string nick = hostmask.Substring(0, hostmask.IndexOf("!", StringComparison.Ordinal));
			string userName = hostmask.Substring(hostmask.IndexOf("!", StringComparison.Ordinal) + 1, hostmask.IndexOf("@", StringComparison.Ordinal));
			string hostname = hostmask.Substring(hostmask.IndexOf("@", StringComparison.Ordinal) + 1);
			string patternNick = pattern.Substring(0, pattern.IndexOf("!", StringComparison.Ordinal));
			string patternUserName = pattern.Substring(pattern.IndexOf("!", StringComparison.Ordinal) + 1, pattern.IndexOf("@", StringComparison.Ordinal));
			string patternHostname = pattern.Substring(pattern.IndexOf("@", StringComparison.Ordinal) + 1);
			return hostname.WildCardMatch(patternHostname) && userName.WildCardMatch(patternUserName) && nick.WildCardMatch(patternNick);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte GetIRCColor(this ColorMappings.Color color)=>ColorMappings.ColorDictionary[color].Item1;
	}
}
