using System;
using dIRCordCS.Config;

namespace dIRCordCS.Bridge{
	public abstract class Listener{
		public readonly byte ConfigID;
		public ref Configuration Config=> ref Program.Config[ConfigID];

		public Listener(byte configId){
			ConfigID = configId;
		}

		protected abstract void ExitHandler(object sender, EventArgs args);

		public abstract void Rehash(ref Configuration newConfig, ref Configuration oldConfig);
	}
}
