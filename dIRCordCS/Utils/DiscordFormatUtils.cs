using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jering.Javascript.NodeJS;

namespace dIRCordCS.Utils{
	public partial class DiscordUtils{
		static DiscordUtils(){
			var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\DiscordFormatting\");
			StaticNodeJSService.Configure<NodeJSProcessOptions>(options=>options.ProjectPath = directoryInfo.FullName);
		}
		public static async Task<string> ConvertFormatting(string message){
			message = message.Replace("{", "{{").Replace("}", "}}");
			string javascriptModule =
				@"const { parser, htmlOutput, toHTML } = require('discord-markdown');module.exports = (callback, message) => {  var result = toHTML(message); callback(null, result); }";
			// Invoke javascript
			message = await StaticNodeJSService.InvokeFromStringAsync<string>(javascriptModule, args: new object[]{message});
			Logger.Debug("discord-markdown: " + message);
			ExtractCode(ref message);
			/*if(Program.Config.PasteBinDevKey != null){
				List<string> codeblocks = message.SubstringsBetween("```");
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
						}
						else{
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
					}
					catch(IOException e){
						Logger.Error("Problem uploading gist", e);
					}
				}
			}*/
			ReplaceTag(ref message, "strong", $"{IrcUtils.BoldChar}");
			ReplaceTag(ref message, "em", $"{IrcUtils.ItalicsChar}");
			ReplaceTag(ref message, "u", $"{IrcUtils.UnderlineChar}");
			//ReplaceTag(ref result, "del", $"{IrcUtils.StrikethroughChar}"); Irc doesn't have a standard IRC char
			ReplaceTag(ref message, "del", "~~"); // Keep Discord formatting
			ReplaceTag(ref message, "code", $"{IrcUtils.ReverseChar}");
			ReplaceTag(ref message, "br", "\n");
			ReplaceTag(ref message, "a", "");
			Logger.Debug($"Formatted as: {message}");
			string decoded = WebUtility.HtmlDecode(message);
			Logger.Debug($"Decoded as: {decoded}");
			return decoded.Trim();
		}
		private static void ReplaceTag(ref string formatted, string tag, string format){
			formatted = Regex.Replace(formatted, $"<\\/?{tag}.*?>", format);
		}
		private static void ReplaceOpenTag(ref string formatted, string tag, string format){
			formatted = Regex.Replace(formatted, $"<{tag}.*?>", format);
		}
		private static void ReplaceCloseTag(ref string formatted, string tag, string format){
			formatted = Regex.Replace(formatted, $"<\\/{tag}.*?>", format);
		}
		public static List<(string lang, string code)> ExtractCode(ref string formatted){
			List<(string lang, string code)> code = new List<(string, string)>();
			const string pattern = @"<pre><code\sclass=""hljs\s?(?<lang>.*?)"">(?<code>.*?)<\/code><\/pre>";

			// Instantiate the regular expression object.
			var r = new Regex(pattern, RegexOptions.IgnoreCase);

			// Match the regular expression pattern against a text string.
			Match m = r.Match(formatted);
			while(m.Success){
				code.Add((m.Groups[1].Value, m.Groups[2].Value));
				m = m.NextMatch();
			}

			formatted = r.Replace(formatted, string.Empty);
			return code;
		}
	}
}
