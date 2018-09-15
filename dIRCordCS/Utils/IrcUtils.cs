using System;
using System.Runtime.CompilerServices;
using ChatSharp;
using DSharpPlus.Entities;
using FuzzyString;

namespace dIRCordCS.Utils{
	public static class IrcUtils{
		public const char ctcpChar = '\u0001';
		public const char colorChar = '\u0003';

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToCTCP(this string str){
			return $"{ctcpChar}{str}{ctcpChar}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsCTCP(this string str){
			return str.StartsWith(ctcpChar.ToString()) &&
			       str.EndsWith(ctcpChar.ToString());
		}

		public static string SanitizeForIRC(this string str, char replaceWith = ' '){
			str = str.Replace('\r', replaceWith).Replace('\n', replaceWith);
			return str;
		}

		public static char GetSymbol(this char mode){
			switch(mode){
			case 'q': return '~';
			case 'a': return '&';
			case 'o': return '@';
			case 'h': return '%';
			case 'v': return '+';
			}

			return '\0';
		}

		public static bool Contains(this string str, char ch){
			foreach(char c in str){
				if(c == ch){
					return true;
				}
			}

			return false;
		}

		public static char GetUserLevel(string levels){
			if(levels.Contains('q'))
				return 'q';
			if(levels.Contains('a'))
				return 'a';
			if(levels.Contains('o'))
				return 'o';
			if(levels.Contains('h'))
				return 'h';
			if(levels.Contains('v'))
				return 'v';
			return '\0';
		}

		public static string GetUserSymbol(IrcUser user){
			return GetUserLevel(user.Mode).GetSymbol().ToString();
		}

		public static string FormatName(IrcUser user, bool useHostmask = false){
			string ret = user.Nick;
			if(useHostmask){
				ret = user.Hostmask;
			}

			if(user.Mode.Contains('o')){
				return "__" + ret + "__";
			}

			return ret;
		}

		public static bool MatchHostMask(this string hostmask, string pattern){
			var nick = hostmask.Substring(0, hostmask.IndexOf("!", StringComparison.Ordinal));
			var userName = hostmask.Substring(hostmask.IndexOf("!", StringComparison.Ordinal) + 1, hostmask.IndexOf("@", StringComparison.Ordinal));
			var hostname = hostmask.Substring(hostmask.IndexOf("@", StringComparison.Ordinal) + 1);
			var patternNick = pattern.Substring(0, pattern.IndexOf("!", StringComparison.Ordinal));
			var patternUserName = pattern.Substring(pattern.IndexOf("!", StringComparison.Ordinal) + 1, pattern.IndexOf("@", StringComparison.Ordinal));
			var patternHostname = pattern.Substring(pattern.IndexOf("@", StringComparison.Ordinal) + 1);
			return hostname.wildCardMatch(patternHostname) && userName.wildCardMatch(patternUserName) && nick.wildCardMatch(patternNick);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte GetIRCColor(this ColorMappings.Color color){
			return ColorMappings.colorDictionary[color].Item1;
		}

	}
}
