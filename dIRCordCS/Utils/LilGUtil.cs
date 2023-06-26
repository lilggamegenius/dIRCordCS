using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;

namespace dIRCordCS.Utils;

public static partial class LilGUtil{
	private static readonly Random Rand = new();
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static bool IsLinux{
		get{
			int p = (int)Environment.OSVersion.Platform;
			return p is 4 or 6 or 128;
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
		double k = size / 1024.0;
		double m = k    / 1024.0;
		double g = m    / 1024.0;
		double t = g    / 1024.0;
		if(t > 1) return $"{t} TB";
		if(g > 1) return $"{g} GB";
		if(m > 1) return $"{m} MB";
		if(k > 1) return $"{k} KB";
		return $"{size} B";
	}

	[GeneratedRegex("-?\\d+(\\.\\d+)?")] private static partial Regex IsNumericRegex();
	public static bool IsNumeric(this string str)=>IsNumericRegex().IsMatch(str);

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

	[GeneratedRegex("([^\"]\\S*|\".+?\")\\s*")] private static partial Regex CommandLineParseRegex();
	public static string[] SplitMessage(this string stringToSplit, int amountToSplit = 0, bool removeQuotes = true){
		if(stringToSplit == null){
			return Array.Empty<string>();
		}

		List<string> list = new();
		Match argSep = CommandLineParseRegex().Match(stringToSplit);
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
		} else{
			for(int i = 0; (list.Count > i) || (amountToSplit > i); i++){
				// go through all of the
				list[i] = Regex.Replace(list[i], "\"", "", RegexOptions.Compiled);   // remove quotes left in the string
				list[i] = Regex.Replace(list[i], "''", "\"", RegexOptions.Compiled); // replace double ' to quotes
				// go to next string
			}
		}

		return list.ToArray();
	}

	public static bool ContainsAny(this string check, params string[] contain)=>contain.Any(check.Contains);

	public static bool EqualsAny(this string check, params string[] equal)=>equal.Any(check.Equals);

	public static bool EqualsAnyIgnoreCase(this string check, params string[] equal)=>equal.Any(check.EqualsIgnoreCase);

	public static bool ContainsAnyIgnoreCase(this string check, params string[] equal){return equal.Any(aEqual=>check.ToLower().Contains(aEqual.ToLower()));}

	public static bool StartsWithAny(this string check, params string[] equal)=>equal.Any(check.StartsWith);

	public static bool EndsWithAny(this string check, params string[] equal)=>equal.Any(check.EndsWith);

	[GeneratedRegex("\\*")] private static partial Regex WildCardRegex();
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
		string[] cards = WildCardRegex().Split(pattern);

		// Iterate over the cards.
		foreach(string card in cards){
			int idx = text.IndexOf(card, StringComparison.Ordinal);

			// Card not detected in the text.
			if(idx == -1){
				return false;
			}

			// Move ahead, towards the right of the text.
			text = text[(idx + card.Length)..];
		}

		return true;
	}

	public static void Pause(int time, bool echoTime = true){
		if(echoTime){
			Logger.Debug("Sleeping for " + time + " seconds");
		}

		Thread.Sleep(time * 1000);
	}

	public static void RemoveDuplicates(ref List<string> list){
		List<string> ar = new();
		while(list.Count > 0){
			ar.Add(list[0]);
			string temp = list[0];
			list.RemoveAll(s=>s.Equals(temp));
		}

		list = list.Concat(ar).ToList();
	}

	public static int Hash(this string str, int maxNum){
		int hash = 0;
		foreach(int charCode in str){
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

	public static int CountMatches(this string str, char find){
		int count = 0;
		foreach(char c in str){
			if(c == find){
				count++;
			}
		}

		return count;
	}

	public static int CountMatches(this string str, string find)=>(str.Length - str.Replace(find, "").Length) / find.Length;

	public static bool CheckUrlValid(this string source)=>
		Uri.TryCreate(source, UriKind.Absolute, out Uri uriResult) &&
		((uriResult.Scheme == Uri.UriSchemeHttp) || (uriResult.Scheme == Uri.UriSchemeHttps));

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
			strToReturn.Append(args[argToStartFrom]).Append(' ');
		}

		Logger.Debug("Argument joined to: " + strToReturn);
		return strToReturn.Length == 0
				   ? strToReturn.ToString()
				   : strToReturn.ToString()[..(strToReturn.Length - 1)];
	}

	public static IEnumerable<string> SplitUp(this string str, int maxChunkSize){
		for(int i = 0; i < str.Length; i += maxChunkSize){
			yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
		}
	}

	[GeneratedRegex("((https?|ftp|gopher|telnet|file):((//)|(\\\\))+[\\w\\d:#@%/;$()~_?+-=\\\\.&]*)", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex URLExtractionRegex();
	/**
		 * Returns a list with all links contained in the input
		 */
	public static List<string> ExtractUrls(string text){
		List<string> containedUrls = new();
		Regex pattern = URLExtractionRegex();
		MatchCollection urlMatcher = pattern.Matches(text);
		foreach(Match match in urlMatcher){
			containedUrls.Add(match.Value);
		}

		return containedUrls;
	}
}
