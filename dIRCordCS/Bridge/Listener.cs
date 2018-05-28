using System;
using dIRCordCS.Config;

namespace dIRCordCS.Bridge{
	public abstract class Listener{
		public readonly byte ConfigID;
		public Configuration Config=>Program.Config[ConfigID];

		public Listener(byte configId){
			ConfigID = configId;
		}

		public abstract void Rehash(ref Configuration newConfig, ref Configuration oldConfig);
	}
}
