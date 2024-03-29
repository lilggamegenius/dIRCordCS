using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using GistsApi;
using Jering.Javascript.NodeJS;
using File = GistsApi.File;

namespace dIRCordCS.Utils;

public partial class DiscordUtils{
	private static readonly Dictionary<string, string> Languages = new(){
		{ "java", "java" },
		{ "cpp", "cpp" },
		{ "c", "c" },
		{ "csharp", "cs" }
	};

	static DiscordUtils(){
		var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\DiscordFormatting\");
		Logger.Info($"JS Project path: {directoryInfo.FullName}");
		StaticNodeJSService.Configure<NodeJSProcessOptions>(options=>options.ProjectPath = directoryInfo.FullName);
	}
	public static async Task<string> ConvertFormatting(DiscordMessage discordMessage, byte configId){
		string message = discordMessage.Content.Replace("{", "{{").Replace("}", "}}");
		const string javascriptModule =
			@"const { parser, htmlOutput, toHTML } = require('discord-markdown');module.exports = (callback, message) => {  var result = toHTML(message); callback(null, result); }";
		// Invoke javascript
		message = await StaticNodeJSService.InvokeFromStringAsync<string>(javascriptModule, args: new object[]{ message });
		Logger.Debug("discord-markdown: " + message);
		List<string> URLs = new();
		if(Program.Config.GistClient != null){
			List<(string lang, string code)> codeBlocks = ExtractCode(ref message);
			if(codeBlocks.Count > 0){
				string description =
					$"Code blocks for <{discordMessage.Author.Username}#{discordMessage.Author.Discriminator}> on {discordMessage.Channel.Guild.Name} " +
					$"in #{discordMessage.Channel.Name} at {discordMessage.Timestamp:MM/dd/yyyy h:mm:ss tt}";
				List<Tuple<string, string>> files = new();
				for(int i = 0; i < codeBlocks.Count; i++){
					Languages.TryGetValue(codeBlocks[i].lang, out string ext);
					if(string.IsNullOrEmpty(ext)){
						ext = "txt";
					}

					files.Add(Tuple.Create($"block {i}.{ext}", codeBlocks[i].code));
				}

				GistObject gistObject = await Program.Config.GistClient.CreateAGist(description, false, files);
				bool first = true;
				foreach(File file in gistObject.files){
					if(first){
						URLs.Add(gistObject.html_url);
						first = false;
						continue;
					}

					URLs.Add($"<{file.filename}>");
				}
			}
		}

		ReplaceTag(ref message, "strong", $"{IrcUtils.BoldChar}");
		ReplaceTag(ref message, "em", $"{IrcUtils.ItalicsChar}");
		ReplaceTag(ref message, "u", $"{IrcUtils.UnderlineChar}");
		//ReplaceTag(ref result, "del", $"{IrcUtils.StrikethroughChar}"); Irc doesn't have a standard strikethrough char
		ReplaceTag(ref message, "del", "~~"); // Keep Discord formatting
		ReplaceTag(ref message, "code", $"{IrcUtils.ReverseChar}");
		ReplaceTag(ref message, "br", "\n ");
		ReplaceTag(ref message, "a", "");
		ReplaceMention(ref message, discordMessage, configId);
		Logger.Debug($"Formatted as: {message}");
		string decoded = WebUtility.HtmlDecode(message).Trim();
		Logger.Debug($"Decoded as: {decoded}");
		// ReSharper disable once CoVariantArrayConversion
		return string.Format(decoded, URLs.ToArray());
	}
	private static void ReplaceTag(ref string formatted, string tag, string format)=>formatted = Regex.Replace(formatted, $"<\\/?{tag}.*?>", format);
	private static void ReplaceOpenTag(ref string formatted, string tag, string format)=>formatted = Regex.Replace(formatted, $"<{tag}.*?>", format);
	private static void ReplaceCloseTag(ref string formatted, string tag, string format)=>formatted = Regex.Replace(formatted, $"<\\/{tag}.*?>", format);

	[GeneratedRegex("<span class=\"d-mention .+?\">(.+?)<\\/span>", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex MentionRegex();
	private static void ReplaceMention(ref string message, DiscordMessage discordMessage, byte configId){
		using IEnumerator<DiscordUser> mentionedUsers = discordMessage.MentionedUsers.GetEnumerator();
		mentionedUsers.MoveNext();
		using IEnumerator<DiscordRole> mentionedRoles = discordMessage.MentionedRoles.GetEnumerator();
		mentionedRoles.MoveNext();
		using IEnumerator<DiscordChannel> mentionedChannels = discordMessage.MentionedChannels.GetEnumerator();
		mentionedChannels.MoveNext();
		Regex r = MentionRegex();
		for(Match m = r.Match(message); m.Success; m = m.NextMatch()){
			string mention = m.Groups[1].Value;
			switch(mention[0]){
				case '&' when mentionedRoles.Current != null: // Role
					message = r.Replace(message, "@" + mentionedRoles.Current.FormatRole(), 1);
					mentionedRoles.MoveNext();
					break;
				case '#' when mentionedChannels.Current != null: // Channel
					message = r.Replace(message, "#" + mentionedChannels.Current.Name, 1);
					mentionedChannels.MoveNext();
					break;
				case '@' when mention[1..].IsNumeric() && (mentionedUsers.Current != null): // User
					message = r.Replace(message, "@" + FormatName(discordMessage.Channel.Guild.GetMemberAsync(mentionedUsers.Current.Id).Result, configId), 1);
					mentionedUsers.MoveNext();
					break;
				default:
					message = r.Replace(message, mention, 1);
					break;
			}
		}
	}

	[GeneratedRegex("<pre><code\\sclass=\"hljs\\s?(?<lang>.*?)\">(?<code>.*?)<\\/code><\\/pre>", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex CodeRegex();
	private static List<(string lang, string code)> ExtractCode(ref string formatted){
		List<(string lang, string code)> code = new();

		// Instantiate the regular expression object.
		Regex r = CodeRegex();

		// Match the regular expression pattern against a text string.
		for(Match m = r.Match(formatted); m.Success; m = m.NextMatch()){
			code.Add((m.Groups[1].Value, m.Groups[2].Value));
		}

		for(int i = 0; i < code.Count; i++){
			formatted = r.Replace(formatted, $" {{{i}}} ", 1);
		}

		return code;
	}
}
