namespace dIRCordCS.Utils{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading.Tasks;
	using AngleSharp.Dom;
	using AngleSharp.Html.Dom;
	using AngleSharp.Html.Parser;
	using Common.Logging;
	using DSharpPlus;
	using DSharpPlus.Entities;
	using Jering.Javascript.NodeJS;

	public static class DiscordUtils{
		public const char ZeroWidthSpace = '\u200b';
		public const string Bold = "**";
		public const char Italics = '_';
		public const string Underline = "__";
		public static ColorMappings ColorMappings = new ColorMappings();

		private static readonly Dictionary<string, string> languages = new Dictionary<string, string>{
			{"java", "java"},
			{"cpp", "cpp"},
			{"c", "c"},
			{"csharp", "cs"}
		};

		private static readonly ILog Logger = LogManager.GetLogger(typeof(DiscordUtils));

		static DiscordUtils(){
			//DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\dIRCordCS\DiscordFormatting\");
			var directoryInfo = new DirectoryInfo(@"D:\Lil-G\workspace\dIRCordCS\dIRCordCS\DiscordFormatting");
			StaticNodeJSService.Configure<NodeJSProcessOptions>(options=>options.ProjectPath = directoryInfo.FullName);
			/*bool supportCodeBlocks = false;
			GitHubClient client = new GitHubClient();
			if(Program.Config[0].GithubGistOAuthToken != null){
				supportCodeBlocks = true;
				client.setOAuth2Token(Program.Config[0].GithubGistOAuthToken);
			} else if(Program.Config[0].GithubCreds        != null &&
					  Program.Config[0].GithubCreds.Length >= 2){
				supportCodeBlocks = true;
				client.setCredentials(Program.Config[0].GithubCreds[0], Program.Config[0].GithubCreds[1]);
			}*/
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatBold(this string str)=>$"{Bold}{str}{Bold}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatItalics(this string str)=>$"{Italics}{str}{Italics}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatUnderline(this string str)=>$"{Underline}{str}{Underline}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetHostMask(this DiscordUser user){
			var member = user as DiscordMember;
			if(member != null){
				return $"{member.DisplayName}!{user.Username}@{user.Id}";
			}

			return $"{user.Username}!{user.Username}@{user.Id}";
		}

		public static string FormatName(this DiscordMember user, byte configId, FormatAs format){
			switch(format){
				case FormatAs.EffectiveName: return FormatName(user, configId);
				case FormatAs.NickName:      return FormatName(user, configId, user.Nickname);
				case FormatAs.Username:      return FormatName(user, configId, user.Username);
				case FormatAs.Id:            return FormatName(user, configId, Convert.ToString(user.Id)); // ????
				default:                     return string.Empty;
			}
		}

		public static string FormatName(this DiscordMember user, byte configId, string @override = null){
			if(@override == null){
				@override = user.DisplayName;
			}

			string nameWithSpace = $"{@override[0]}{ZeroWidthSpace}{@override.Substring(1)}";
			if(user.Id == Program.Config.Servers[configId].DiscordBotOwnerID){
				nameWithSpace.ToUnderline();
			}

			byte ircColorCode = ColorMappings[ColorMappings[user.Color]].Item1;
			if(Program.Config.IrcNickColor){
				if(ircColorCode == byte.MaxValue){
					ircColorCode = (byte)(user.Id.ToString().Hash(12) + 2);
				}

				return nameWithSpace.ToColor(ircColorCode);
			}

			return nameWithSpace;
		}

		public static async Task<string> ConvertFormatting(string message){
			message = message.Replace("{", "{{").Replace("}", "}}");
			/*
			if(supportCodeBlocks){
				string[] codeblocks = StringUtils.substringsBetween(message, "```", "```");
				if(codeblocks != null){
					string lang;
					Gist gist = new Gist().setDescription("Discord code block");
					Dictionary<string, GistFile> files = new Dictionary<string, GistFile>();
					for(int i = 0; i < codeblocks.Length; i++){
						lang = codeblocks[i].Substring(0, codeblocks[i].IndexOf('\n'));
						if(lang.Length != 0 ||
						   languages.ContainsKey(lang)){
							GistFile file = new GistFile().setContent(codeblocks[i].Replace(lang + "\n", ""));
							files[$"Block{i}.{languages[lang]}"] = file;
						} else{
							GistFile file = new GistFile().setContent(codeblocks[i]);
							files[$"Block{i}.txt"] = file;
						}
					}

					try{
						gist.setFiles(files);
						gist = new GistService(client).createGist(gist);
						string url = gist.getHtmlUrl();
						byte index = 0;
						foreach(GistFile file in gist.getFiles().values()){
							message = message.Replace($"```{codeblocks[index]}```",
													  $"{url}#file-block{index++}-{file.getFilename().substring(file.getFilename().lastIndexOf('.'))}"
													 );
						}
					} catch(IOException e){
						Logger.Error("Problem uploading gist", e);
					}
				}
			}*/

			// find links
			List<string> parts = LilGUtil.ExtractUrls(message);
			for(int i = 0; i < parts.Count; i++){
				message = message.Replace(parts[i], $"{{{i}}}");
			}

			string encoded = WebUtility.HtmlEncode(message);
			string javascriptModule = @"
const { parser, htmlOutput, toHTML } = require('discord-markdown');
module.exports = (callback, message) => {  // Module must export a function that takes a callback as its first parameter
    var result = toHTML(message); // Your javascript logic
    callback(null /* If an error occurred, provide an error object or message */, result); // Call the callback when you're done.
}";
			// Invoke javascript
			string result = await StaticNodeJSService.InvokeFromStringAsync<string>(javascriptModule, args: new object[]{encoded});
			Logger.Debug(result);
			/*ReplaceTag(ref result, "strong", $"{IrcUtils.BoldChar}");
			ReplaceTag(ref result, "em", $"{IrcUtils.ItalicsChar}");
			ReplaceTag(ref result, "u", $"{IrcUtils.UnderlineChar}");
			//ReplaceTag(ref result, "del", $"{IrcUtils.StrikethroughChar}"); Irc doesn't have a standard IRC char
			ReplaceTag(ref result, "del", "~~");
			ReplaceTag(ref result, "code", $"{IrcUtils.ReverseChar}"); */
			var parser = new HtmlParser();
			IHtmlDocument document = parser.ParseDocument(result);
			string formatted = ConvertTags(document.Body.Children).ToString();
			Logger.Debug($"Formatted as {formatted}");
			string decoded = WebUtility.HtmlDecode(formatted);
			return string.Format(decoded, parts.ToArray());
		}

		public static StringBuilder ConvertTags(IHtmlCollection<IElement> elements){
			var builder = new StringBuilder();
			foreach(IElement element in elements){
				if(element.ChildElementCount > 0){
					builder.Append(ConvertTags(element.Children));
				}

				Logger.Debug(element.InnerHtml);
				builder.Append(element.InnerHtml);
				switch(element.TagName){
					case "strong": break;
					case "em":     break;
					case "u":      break;
					case "del":    break;
					/*case "del":

					break;*/
					case "code": break;
				}
			}

			return builder;
		}

		public static Permissions GetPermissions(this DiscordMember member){
			var permissions = Permissions.None;
			foreach(DiscordRole role in member.Roles){
				permissions |= role.Permissions;
			}

			return permissions;
		}

		public static bool CanInteract(this DiscordMember issuer, DiscordMember target){
			if(issuer == null){
				throw new ArgumentNullException(nameof(issuer));
			}

			if(target == null){
				throw new ArgumentNullException(nameof(target));
			}

			DiscordGuild guild = issuer.Guild;
			if(guild != target.Guild){
				throw new ArgumentException("Provided members must both be Member objects of the same Guild!");
			}

			if(guild.Owner == issuer){
				return true;
			}

			if(guild.Owner == target){
				return false;
			}

			DiscordRole issuerRole = issuer.Roles.FirstOrDefault();
			DiscordRole targetRole = target.Roles.FirstOrDefault();
			return CanInteract(issuerRole, targetRole);
		}

		public static bool CanInteract(this DiscordRole issuer, DiscordRole target){
			if(issuer == null){
				throw new ArgumentNullException(nameof(issuer));
			}

			if(target == null){
				throw new ArgumentNullException(nameof(target));
			}

			return target.Position < issuer.Position;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DiscordColor GetDiscordColor(this ColorMappings.Color color)=>ColorMappings.ColorDictionary[color].Item2;

		public static string ToCommaSeperatedList(this IEnumerable<DiscordRole> array){
			var builder = new StringBuilder();
			foreach(DiscordRole item in array){
				if(builder.Length != 0){
					builder.Append(", ");
				}

				builder.Append(item.Name);
			}

			return builder.ToString();
		}
	}

	public enum FormatAs{ EffectiveName, NickName, Username, Id }

	public class ColorMappings{
		public enum Color : byte{
			Turquoise = 1,
			DarkTurquoise,
			Green,
			DarkGreen,
			Blue,
			DarkBlue,
			Purple,
			DarkPurple,
			Pink,
			DarkPink,
			Yellow,
			DarkYellow,
			Orange,
			DarkOrange,
			Red,
			DarkRed,
			LightGray,
			Gray,
			DarkGray,
			DarkerGray
		}
		public static readonly Dictionary<Color, (byte, DiscordColor)> ColorDictionary = new Dictionary<Color, (byte, DiscordColor)>{
			{Color.Turquoise, (10, new DiscordColor(26, 188, 156))},
			{Color.DarkTurquoise, (10, new DiscordColor(17, 128, 106))},
			{Color.Green, (9, new DiscordColor(46, 204, 113))},
			{Color.DarkGreen, (3, new DiscordColor(31, 139, 76))},
			{Color.Blue, (10, new DiscordColor(52, 152, 219))},
			{Color.DarkBlue, (2, new DiscordColor(32, 102, 148))},
			{Color.Purple, (13, new DiscordColor(155, 89, 182))},
			{Color.DarkPurple, (6, new DiscordColor(113, 54, 138))},
			{Color.Pink, (13, new DiscordColor(233, 30, 99))},
			{Color.DarkPink, (6, new DiscordColor(173, 20, 87))},
			{Color.Yellow, (8, new DiscordColor(241, 196, 15))},
			{Color.DarkYellow, (8, new DiscordColor(194, 124, 14))},
			{Color.Orange, (7, new DiscordColor(230, 126, 34))},
			{Color.DarkOrange, (7, new DiscordColor(168, 67, 0))},
			{Color.Red, (4, new DiscordColor(231, 76, 60))},
			{Color.DarkRed, (5, new DiscordColor(153, 45, 34))},
			{Color.LightGray, (0, new DiscordColor(149, 165, 166))},
			{Color.Gray, (15, new DiscordColor(151, 156, 159))},
			{Color.DarkGray, (14, new DiscordColor(96, 125, 139))},
			{Color.DarkerGray, (1, new DiscordColor(84, 110, 122))}
		};

		public Color this[byte number]{
			get{
				foreach(Color color in ColorDictionary.Keys){
					if(ColorDictionary[color].Item1 == number){
						return color;
					}
				}

				return 0;
			}
		}

		public Color this[DiscordColor findDiscordColor]{
			get{
				foreach(Color color in ColorDictionary.Keys){
					if(ColorDictionary[color].Item2.Equals(findDiscordColor)){
						return color;
					}
				}

				return 0;
			}
		}

		public (byte, DiscordColor) this[Color color]=>!ColorDictionary.ContainsKey(color) ? (byte.MaxValue, DiscordColor.None) : ColorDictionary[color];
	}
}
