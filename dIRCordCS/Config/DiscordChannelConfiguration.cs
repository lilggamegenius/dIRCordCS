namespace dIRCordCS.Config;

public struct DiscordChannelConfiguration : IChannelConfiguration{
#pragma warning disable 649
	private readonly string[] _commandCharacters;
#pragma warning restore 649

	public string[] GetCommmandCharacters()=>_commandCharacters;
}
