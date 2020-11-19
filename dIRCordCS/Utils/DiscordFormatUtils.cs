using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jering.Javascript.NodeJS;

namespace dIRCordCS.Utils{
	public partial class DiscordUtils{
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
		public static async Task<string> ConvertFormatting(string message){
			message = message.Replace("{", "{{").Replace("}", "}}");

			// find links
			// List<string> parts = LilGUtil.ExtractUrls(message);
			// for(int i = 0; i < parts.Count; i++){
			// 	message = message.Replace(parts[i], $"{{{i}}}");
			// }
			string javascriptModule =
				@"const { parser, htmlOutput, toHTML } = require('discord-markdown');module.exports = (callback, message) => {  var result = toHTML(message); callback(null, result); }";
			// Invoke javascript
			message = await StaticNodeJSService.InvokeFromStringAsync<string>(javascriptModule, args: new object[]{message});
			Logger.Debug("discord-markdown: " + message);
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
			ReplaceTag(ref message, "del", "~~");
			ReplaceTag(ref message, "code", $"{IrcUtils.ReverseChar}");
			ReplaceTag(ref message, "br", "\n");
			ReplaceTag(ref message, "a", "");
			Logger.Debug($"Formatted as: {message}");
			string decoded = WebUtility.HtmlDecode(message);
			Logger.Debug($"Decoded as: {decoded}");
			return decoded;
			//return string.Format(decoded, parts.ToArray());
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
		public static List<string> GetLanguages(string formatted){
			List<string> langs = new List<string>();
			const string pattern = @"<pre><code class=""hljs (.*)"">.*<\/code><\/pre>";

			// Instantiate the regular expression object.
			var r = new Regex(pattern, RegexOptions.IgnoreCase);

			// Match the regular expression pattern against a text string.
			Match m = r.Match(formatted);
			int matchCount = 0;
			while(m.Success){
				Console.WriteLine("Match" + ++matchCount);
				for(int i = 1; i <= 2; i++){
					Group g = m.Groups[i];
					CaptureCollection cc = g.Captures;
					for(int j = 0; j < cc.Count; j++){
						Capture c = cc[j];
						langs.Add(c.Value);
					}
				}

				m = m.NextMatch();
			}

			return langs;
		}

		public static List<string> SubstringsBetween(this string str, string find){
			List<string> strings = new List<string>();
			for(int i = 0; i < str.Length; i++){
				int index = str.IndexOf(find, StringComparison.Ordinal);
				if(index == -1){
					break;
				}
			}

			return strings;
		}
	}
}
