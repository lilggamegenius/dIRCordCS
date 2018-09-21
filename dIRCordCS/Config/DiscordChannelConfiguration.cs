namespace dIRCordCS.Config{
	public struct DiscordChannelConfiguration : IChannelConfiguration{
		#pragma warning disable 649
		readonly string[] commandCharacters;
		#pragma warning restore 649

		public string[] getCommmandCharacters(){return commandCharacters;}
	}
}
