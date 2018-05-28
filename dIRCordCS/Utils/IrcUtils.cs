using System;
using System.Runtime.CompilerServices;
using IrcDotNet.Collections;
using IrcDotNet.Target.Channel;
using IrcDotNet.Target.User;

namespace dIRCordCS.Utils{
	public static class IrcUtils{
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

		public static char GetUserLevel(ReadOnlySet<char> levels){
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

		public static string GetUserSymbol(IrcChannelUser user){
			return GetUserLevel(user.Modes).GetSymbol().ToString();
		}

		public static string FormatName(IrcUser user, bool useHostmask = false){
			string ret = user.NickName;
			if(useHostmask){
				ret = user.HostMask;
			}

			if(user.IsOperator){
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
			if(!nick.wildCardMatch(patternNick))
				return false;
			return userName.wildCardMatch(patternUserName) && hostname.wildCardMatch(patternHostname);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte GetIRCColor(this ColorMappings.Color color){
			return ColorMappings.colorDictionary[color].Item1;
		}

	}
}
