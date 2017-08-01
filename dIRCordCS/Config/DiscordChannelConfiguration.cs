namespace dIRCordCS.Config{
	public struct DiscordChannelConfiguration : IChannelConfiguration{
		readonly string[] commandCharacters;

		public string[] getCommmandCharacters() {
			return commandCharacters;
		}
	}
}