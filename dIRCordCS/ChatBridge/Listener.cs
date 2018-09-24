using System;
using dIRCordCS.Config;

namespace dIRCordCS.ChatBridge{
	public abstract class Listener{
		public readonly byte ConfigId;

		public Listener(byte configId)=>ConfigId = configId;
		public ref Configuration Config=>ref Program.Config[ConfigId];

		protected abstract void ExitHandler(object sender, EventArgs args);

		public abstract void Rehash(ref Configuration newConfig, ref Configuration oldConfig);
	}
}
