using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.google.common.collect;
using Common.Logging;
using dIRCordCS.Listeners;
using DSharpPlus;
using DSharpPlus.Entities;
using ikvm.extensions;
using java.lang.management;
using java.text;
using java.util;
using java.util.regex;
using javax.management;
using sun.misc;
using Attribute = javax.management.Attribute;
using Double = java.lang.Double;
using GC = System.GC;
using Random = System.Random;

namespace dIRCordCS.Utils{
	public static class LilGUtil{
		private static readonly Random Rand = new Random();
		private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

		public static bool IsLinux{
			get{
				var p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}
		/**
		 * Returns a pseudo-random number between min and max, inclusive.
		 * The difference between min and max can be at most
		 * <code>Integer.MAX_VALUE - 1</code>.
		 *
		 * @param min Minimum value
		 * @param max Maximum value.  Must be greater than min.
		 * @return Integer between min and max, inclusive.
		 * @see java.util.Random#nextInt(int)
		 */

		public static int randInt(int min, int max){
			// nextInt is normally exclusive of the top value,
			// so add 1 to make it inclusive
			return Rand.Next(max - min + 1) + min;
		}

		public static double randDec(double min, double max){
			// nextInt is normally exclusive of the top value,
			// so add 1 to make it inclusive
			return Rand.NextDouble() * (max - min) + min;
		}

		public static string getBytes(string byteStr){
			var bytes = byteStr.getBytes();
			return Arrays.toString(bytes);
		}

		public static string formatFileSize(long size){
			string hrSize;
			var k = size / 1024.0;
			var m = size / 1024.0 / 1024.0;
			var g = size / 1024.0 / 1024.0 / 1024.0;
			var t = size / 1024.0 / 1024.0 / 1024.0 / 1024.0;
			var dec = new DecimalFormat("0.00");
			if(t > 1){
				hrSize = dec.format(t).concat(" TB");
			}
			else if(g > 1){
				hrSize = dec.format(g).concat(" GB");
			}
			else if(m > 1){
				hrSize = dec.format(m).concat(" MB");
			}
			else if(k > 1){
				hrSize = dec.format(k).concat(" KB");
			}
			else{
				hrSize = dec.format((double)size).concat(" B");
			}

			return hrSize;
		}

		public static bool isNumeric(this string str){
			return str.matches("-?\\d+(\\.\\d+)?"); //match a number with optional '-' and decimal.
		}

		/**
		 * This method guarantees that garbage collection is
		 * done unlike <code>{@link System#gc()}</code>
		 */
		public static int gc(){
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Logger.Info("GC ran");
			return 0;
		}

		public static Unsafe getUnsafe(){
			return Unsafe.getUnsafe();
		}

		public static long sizeOf(object obj){
			return -1;
		}

		public static string[] splitMessage(this string stringToSplit, int amountToSplit = 0, bool removeQuotes = true){
			if(stringToSplit == null)
				return new string[0];
			var list = new List<string>();
			var argSep = Pattern.compile("([^\"]\\S*|\".+?\")\\s*").matcher(stringToSplit);
			while(argSep.find())
				list.Add(argSep.group(1));
			if(!removeQuotes)
				return list.ToArray();
			if(amountToSplit != 0){
				for(var i = 0; list.Count > i; i++){
					// go through all of the
					list[i] = list[i].replaceAll("\"", ""); // remove quotes left in the string
					list[i] = list[i].replaceAll("''", "\""); // replace double ' to quotes
					// go to next string
				}
			}
			else{
				for(var i = 0; list.Count > i || amountToSplit > i; i++){
					// go through all of the
					list[i] = list[i].replaceAll("\"", ""); // remove quotes left in the string
					list[i] = list[i].replaceAll("''", "\""); // replace double ' to quotes
					// go to next string
				}
			}

			return list.ToArray();
		}

		public static bool containsAny(this string check, params string[] contain){
			return contain.Any(aContain=>check.contains(aContain));
		}

		public static bool equalsAny(this string check, params string[] equal){
			return equal.Any(aEqual=>check == aEqual);
		}

		public static bool equalsAnyIgnoreCase(this string check, params string[] equal){
			return equal.Any(check.equalsIgnoreCase);
		}

		public static bool containsAnyIgnoreCase(this string check, params string[] equal){
			return equal.Any(aEqual=>check.toLowerCase().contains(aEqual.toLowerCase()));
		}

		public static bool startsWithAny(this string check, params string[] equal){
			return equal.Any(check.startsWith);
		}

		public static bool endsWithAny(this string check, params string[] equal){
			return equal.Any(check.endsWith);
		}

		/**
		 * Performs a wildcard matching for the text and pattern
		 * provided.
		 *
		 * @param text    the text to be tested for matches.
		 * @param pattern the pattern to be matched for.
		 *                This can contain the wildcard character '*' (asterisk).
		 * @return <tt>true</tt> if a match is found, <tt>false</tt>
		 * otherwise.
		 */
		public static bool wildCardMatch(this string text, string pattern){
			// Create the cards by splitting using a RegEx. If more speed
			// is desired, a simpler character based splitting can be done.
			var cards = pattern.split("\\*");

			// Iterate over the cards.
			foreach(var card in cards){
				var idx = text.indexOf(card);

				// Card not detected in the text.
				if(idx == -1){
					return false;
				}

				// Move ahead, towards the right of the text.
				text = text.substring(idx + card.length());
			}

			return true;
		}

		public static bool MatchHostMask(this string hostmask, string pattern){
			var nick = hostmask.substring(0, hostmask.indexOf("!"));
			var userName = hostmask.substring(hostmask.indexOf("!") + 1, hostmask.indexOf("@"));
			var hostname = hostmask.substring(hostmask.indexOf("@") + 1);
			var patternNick = pattern.substring(0, pattern.indexOf("!"));
			var patternUserName = pattern.substring(pattern.indexOf("!") + 1, pattern.indexOf("@"));
			var patternHostname = pattern.substring(pattern.indexOf("@") + 1);
			if(!wildCardMatch(nick, patternNick))
				return false;
			if(!wildCardMatch(userName, patternUserName))
				return false;
			return wildCardMatch(hostname, patternHostname);
		}

		public static void pause(int time, bool echoTime = true){
			if(echoTime){
				Logger.Debug("Sleeping for " + time + " seconds");
			}

			Thread.Sleep(time * 1000);
		}

//        public static T searchEnum<T>(Class<T> enumeration, string search) where T:Enum {
//            for (T each : enumeration.getEnumConstants()) {
//                if (each.name().equalsIgnoreCase(search)) {
//                    return each;
//                }
//            }
//            return null;
//        }

		public static void removeDuplicates(ref List<string> list){
			var ar = new List<string>();
			while(list.Count > 0){
				ar.Add(list[0]);
				var temp = list[0];
				list.RemoveAll(s=>s.Equals(temp));
			}

			list = list.Concat(ar).ToList();
		}

//    public static  S cast<T, S>(T type, Class<S> cast) where S:object T:object {
//        return cast.cast(type);
//    }

		public static int hash(this string str, int maxNum){
			var hash = 0;
			for(var i = 0; i < str.length(); i++){
				int charCode = str.charAt(i);
				hash += charCode;
			}

			return hash % maxNum;
		}

		public static double getProcessCpuLoad(){
			var mbs = ManagementFactory.getPlatformMBeanServer();
			var name = ObjectName.getInstance("java.lang:type=OperatingSystem");
			var list = mbs.getAttributes(name, new[]{"ProcessCpuLoad"});
			if(list.isEmpty())
				return Double.NaN;
			var att = (Attribute)list.get(0);
			var value = (double)att.getValue();

			// usually takes a couple of seconds before we get real values
			if(value == -1.0)
				return Double.NaN;
			// returns a percentage value with 1 decimal point precision
			return (int)(value * 1000) / 10.0;
		}

		public static List<T> toList<T>(this List javaList){
			var ret = new List<T>();
			for(var i = 0; i < javaList.size(); i++){
				ret[i] = (T)javaList.get(i);
			}

			return ret;
		}

		public static List<T> toList<T>(this ImmutableSortedSet immutableSortedSet){
			var ret = new List<T>();
			var iter = immutableSortedSet.iterator();
			for(var i = 0; iter.hasNext(); i++){
				ret[i] = (T)iter.next();
			}

			return ret;
		}

		public static List toJList<T>(this List<T> list){
			var jList = new ArrayList();
			foreach(var i in list){
				jList.add(i);
			}

			return jList;
		}

		public static int countMatches(this string str, char find){
			var count = 0;
			foreach(var c in str)
				if(c == find)
					count++;
			return count;
		}

		public static int countMatches(this string str, string find){
			return (str.Length - str.Replace(find, "").Length) / find.Length;
		}

		public static bool CheckURLValid(this string source){
			return Uri.TryCreate(source, UriKind.Absolute, out Uri uriResult) &&
			       (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		}

		/*public static string EffectiveName(this IGuildUser user){
		    return user.Nickname ?? user.Username;
		}*/

		public static bool EqualsIgnoreCase(this string first, string second){
			if(first == null ||
			   second == null)
				return first == second;
			return first.ToLower().Equals(second.ToLower());
		}

		public static IEnumerable<string> SplitUp(this string str, int maxChunkSize){
			for(int i = 0; i < str.Length; i += maxChunkSize)
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
		}

		public static string GetHostMask(this DiscordMember member){
			return $"{member.DisplayName}!{member.Username}@{member.Id}";
		}

		public static Permissions GetPermissions(this DiscordMember member){
			Permissions permissions = Permissions.None;
			foreach(DiscordRole role in member.Roles){
				permissions |= role.Permissions;
			}

			return permissions;
		}

		public static bool CanInteract(this DiscordMember issuer, DiscordMember target){
			if(issuer == null) throw new ArgumentNullException(nameof(issuer));
			if(target == null) throw new ArgumentNullException(nameof(target));

			DiscordGuild guild = issuer.Guild;
			if(guild != target.Guild) throw new ArgumentException("Provided members must both be Member objects of the same Guild!");
			if(guild.Owner == issuer) return true;
			if(guild.Owner == target) return false;
			DiscordRole issuerRole = issuer.Roles.FirstOrDefault();
			DiscordRole targetRole = target.Roles.FirstOrDefault();
			return issuerRole == null && (targetRole == null || CanInteract(issuerRole, targetRole));
		}

		public static bool CanInteract(this DiscordRole issuer, DiscordRole target){
			if(issuer == null) throw new ArgumentNullException(nameof(issuer));
			if(target == null) throw new ArgumentNullException(nameof(target));
			return target.Position < issuer.Position;
		}

		public static byte GetIRCColor(this ColorMappings.Color color){
			return ColorMappings.colorDictionary[color].Item1;
		}

		public static DiscordColor GetDiscordColor(this ColorMappings.Color color){
			return ColorMappings.colorDictionary[color].Item2;
		}
	}
}
