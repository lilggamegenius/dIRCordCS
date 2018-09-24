using System.Collections.Generic;
using Newtonsoft.Json;

namespace dIRCordCS.Config{
	public struct IRCChannelConfiguration : IChannelConfiguration{
		public readonly bool joins;
		public readonly bool quits;
		public readonly bool parts;
		public readonly string[] commandCharacters;
		[JsonIgnore] public readonly List<string> spamFilterList;

		public string[] GetCommmandCharacters()=>commandCharacters;
	}
}
