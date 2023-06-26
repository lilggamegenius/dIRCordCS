using System;
using dIRCordCS.Config;

namespace dIRCordCS.ChatBridge;

public abstract class Listener{
	public readonly byte ConfigId;

	public Listener(byte configId)=>ConfigId = configId;
	public Configuration.ServerConfigs Config=>Program.Config.Servers[ConfigId];

	protected abstract void ExitHandler(object sender, EventArgs args);

	public abstract void Rehash(Configuration.ServerConfigs newConfig, Configuration.ServerConfigs oldConfig);
}
