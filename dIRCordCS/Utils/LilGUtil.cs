using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;

namespace dIRCordCS.Utils{
	public static class LilGUtil{
		private static readonly Random Rand = new();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static bool IsLinux{
			get{
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}
		/**
		 * Returns a pseudo-random number between min and max, inclusive.
		 * The difference between min and max can be at most
		 * <code>Integer.MAX_VALUE - 1</code>
		 * .
		 * 
		 * @param min Minimum value
		 * @param max Maximum value.  Must be greater than min.
		 * @return Integer between min and max, inclusive.
		 * @see java.util.Random#nextInt(int)
		 */
		public static int RandInt(int min, int max)=>Rand.Next((max - min) + 1) + min;

		public static double RandDec(double min, double max)=>(Rand.NextDouble() * (max - min)) + min;

		public static string GetBytes(this string byteStr){
			char[] bytes = byteStr.ToCharArray();
			return bytes.ToString();
		}

		public static string FormatFileSize(long size){
			string hrSize;
			double k = size / 1024.0;
			double m = size / 1024.0 / 1024.0;
			double g = size / 1024.0 / 1024.0 / 1024.0;
			double t = size / 1024.0 / 1024.0 / 1024.0 / 1024.0;
			if(t > 1){
				hrSize = $"{t} TB";
			}
			else{
				if(g > 1){
					hrSize = $"{g} GB";
				}
				else{
					if(m > 1){
						hrSize = $"{m} MB";
					}
					else{
						if(k > 1){
							hrSize = $"{k} KB";
						}
						else{
							hrSize = $"{size} B";
						}
					}
				}
			}

			return hrSize;
		}

		public static bool IsNumeric(this string str)=>Regex.IsMatch(str, "-?\\d+(\\.\\d+)?");

		/**
		 * This method guarantees that garbage collection is
		 * done unlike
		 * <code>{@link System#gc()}</code>
		 */
		public static int Gc(){
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Logger.Info("GC ran");
			return 0;
		}

		/*public static Unsafe getUnsafe(){
			return Unsafe.getUnsafe();
		}*/

		public static long SizeOf(object obj)=>-1;

		public static string[] SplitMessage(this string stringToSplit, int amountToSplit = 0, bool removeQuotes = true){
			if(stringToSplit == null){
				return new string[0];
			}

			List<string> list = new();
			Match argSep = Regex.Match(stringToSplit, "([^\"]\\S*|\".+?\")\\s*");
			foreach(Capture match in argSep.Captures){
				list.Add(match.Value);
			}

			if(!removeQuotes){
				return list.ToArray();
			}

			if(amountToSplit != 0){
				for(int i = 0; list.Count > i; i++){
					// go through all of the
					list[i] = Regex.Replace(list[i], "\"", "", RegexOptions.Compiled);   // remove quotes left in the string
					list[i] = Regex.Replace(list[i], "''", "\"", RegexOptions.Compiled); // replace double ' to quotes
					// go to next string
				}
			}
			else{
				for(int i = 0; (list.Count > i) || (amountToSplit > i); i++){
					// go through all of the
					list[i] = Regex.Replace(list[i], "\"", "", RegexOptions.Compiled);   // remove quotes left in the string
					list[i] = Regex.Replace(list[i], "''", "\"", RegexOptions.Compiled); // replace double ' to quotes
					// go to next string
				}
			}

			return list.ToArray();
		}

		private static string argJoiner(string[] args, int argToStartFrom = 0){
			if((args.Length - 1) == argToStartFrom){
				return args[argToStartFrom];
			}

			var strToReturn = new StringBuilder();
			for(int length = args.Length; length > argToStartFrom; argToStartFrom++){
				strToReturn.Append(args[argToStartFrom]).Append(" ");
			}

			Logger.Debug($"Argument joined to: {strToReturn}");
			return strToReturn.Length == 0 ? strToReturn.ToString() : strToReturn.ToString().Substring(0, strToReturn.Length - 1);
		}

		public static bool ContainsAny(this string check, params string[] contain)=>contain.Any(check.Contains);

		public static bool EqualsAny(this string check, params string[] equal)=>equal.Any(check.Equals);

		public static bool EqualsAnyIgnoreCase(this string check, params string[] equal)=>equal.Any(check.EqualsIgnoreCase);

		public static bool ContainsAnyIgnoreCase(this string check, params string[] equal){
			return equal.Any(aEqual=>check.ToLower().Contains(aEqual.ToLower()));
		}

		public static bool StartsWithAny(this string check, params string[] equal)=>equal.Any(check.StartsWith);

		public static bool EndsWithAny(this string check, params string[] equal)=>equal.Any(check.EndsWith);

		/**
		 * Performs a wildcard matching for the text and pattern
		 * provided.
		 * 
		 * @param text    the text to be tested for matches.
		 * @param pattern the pattern to be matched for.
		 * This can contain the wildcard character '*' (asterisk).
		 * @return
		 * <tt>true</tt>
		 * if a match is found,
		 * <tt>false</tt>
		 * otherwise.
		 */
		public static bool WildCardMatch(this string text, string pattern){
			// Create the cards by splitting using a RegEx. If more speed
			// is desired, a simpler character based splitting can be done.
			string[] cards = Regex.Split(pattern, "\\*");

			// Iterate over the cards.
			foreach(string card in cards){
				int idx = text.IndexOf(card, StringComparison.Ordinal);

				// Card not detected in the text.
				if(idx == -1){
					return false;
				}

				// Move ahead, towards the right of the text.
				text = text.Substring(idx + card.Length);
			}

			return true;
		}

		public static void Pause(int time, bool echoTime = true){
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

		public static void RemoveDuplicates(ref List<string> list){
			List<string> ar = new();
			while(list.Count > 0){
				ar.Add(list[0]);
				string temp = list[0];
				list.RemoveAll(s=>s.Equals(temp));
			}

			list = list.Concat(ar).ToList();
		}

//    public static  S cast<T, S>(T type, Class<S> cast) where S:object T:object {
//        return cast.cast(type);
//    }

		public static int Hash(this string str, int maxNum){
			int hash = 0;
			for(int i = 0; i < str.Length; i++){
				int charCode = str[i];
				hash += charCode;
			}

			return hash % maxNum;
		}

		public static string ToCommaSeperatedList<T>(this T[] array){
			var builder = new StringBuilder();
			foreach(T item in array){
				if(builder.Length != 0){
					builder.Append(", ");
				}

				builder.Append(item);
			}

			return builder.ToString();
		}

		public static string ToCommaSeperatedList<T>(this IEnumerable<T> array){
			var builder = new StringBuilder();
			foreach(T item in array){
				if(builder.Length != 0){
					builder.Append(", ");
				}

				builder.Append(item);
			}

			return builder.ToString();
		}

		/*public static double getProcessCpuLoad(){
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
		}*/

		/*public static List<T> toList<T>(this List javaList){
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
		}*/

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int CountMatches(this string str, char find){
			int count = 0;
			foreach(char c in str){
				if(c == find){
					count++;
				}
			}

			return count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int CountMatches(this string str, string find)=>(str.Length - str.Replace(find, "").Length) / find.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CheckUrlValid(this string source)=>
			Uri.TryCreate(source, UriKind.Absolute, out Uri uriResult) &&
			((uriResult.Scheme == Uri.UriSchemeHttp) || (uriResult.Scheme == Uri.UriSchemeHttps));

		/*public static string EffectiveName(this IGuildUser user){
		    return user.Nickname ?? user.Username;
		}*/

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EqualsIgnoreCase(this string first, string second){
			if((first  == null) ||
			   (second == null)){
				return first == second;
			}

			return first.ToLower().Equals(second.ToLower());
		}

		public static string ArgJoiner(string[] args, int argToStartFrom = 0){
			if((args.Length - 1) == argToStartFrom){
				return args[argToStartFrom];
			}

			var strToReturn = new StringBuilder();
			for(int length = args.Length; length > argToStartFrom; argToStartFrom++){
				strToReturn.Append(args[argToStartFrom]).Append(" ");
			}

			Logger.Debug("Argument joined to: " + strToReturn);
			return strToReturn.Length == 0
					   ? strToReturn.ToString()
					   : strToReturn.ToString().Substring(0, strToReturn.Length - 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<string> SplitUp(this string str, int maxChunkSize){
			for(int i = 0; i < str.Length; i += maxChunkSize){
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
			}
		}

		/**
		 * Returns a list with all links contained in the input
		 */
		public static List<string> ExtractUrls(string text){
			List<string> containedUrls = new();
			const string urlRegex = "((https?|ftp|gopher|telnet|file):((//)|(\\\\))+[\\w\\d:#@%/;$()~_?+-=\\\\.&]*)";
			var pattern = new Regex(urlRegex, RegexOptions.IgnoreCase);
			MatchCollection urlMatcher = pattern.Matches(text);
			foreach(Match match in urlMatcher){
				containedUrls.Add(match.Value);
			}

			return containedUrls;
		}
	}
}
