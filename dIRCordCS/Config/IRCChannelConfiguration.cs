namespace dIRCordCS.Config{
	public struct IRCChannelConfiguration : IChannelConfiguration{
		public readonly bool joins;
		public readonly bool quits;
		public readonly bool parts;
		public readonly string[] commandCharacters;

		public string[] getCommmandCharacters() {
			return commandCharacters;
		}
		
	}
}