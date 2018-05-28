using System;
using dIRCordCS.Config;

namespace dIRCordCS.Bridge{
	public class DiscordListener : Listener{
		public DiscordListener(byte configId) : base(configId){

		}

		public override void Rehash(ref Configuration newConfig, ref Configuration oldConfig){}
	}
}
