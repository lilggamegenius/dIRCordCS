using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcDotNet;
using IrcDotNet.Network;

namespace dIRCordCS.Utils{
	public class IrcClientManager : IDisposable{
		private List<StandardIrcClient> clients;

		public StandardIrcClient this[int index]=>clients[index];

		public void Add(StandardIrcClient client){
			clients.Add(client);
		}

		public void Start(IrcConfig config){
			Parallel.ForEach(config.configs,
			                 conf=>{
				                 var client = new StandardIrcClient();
				                 clients.Add(client);
				                 client.Connect(conf.Item2, conf.Item3, conf.Item4, conf.Item1);
			                 }
			                 );
		}

		public void Stop(string message = null){
			Parallel.ForEach(clients,
			                 client=>{
				                 client.Quit(message);
			                 }
			                );
		}

		public void Dispose(){
			Stop("Disposing");
		}
	}

	public class IrcConfig{
		internal Stack<Tuple<IrcRegistrationInfo, string, int, bool>> configs;

		public void Add(IrcRegistrationInfo config, string address, int port = 6667, bool useSSL = false){
			Add(new Tuple<IrcRegistrationInfo, string, int, bool>(config, address, port, useSSL));
		}

		public void Add(Tuple<IrcRegistrationInfo, string, int, bool> config){
			configs.Push(config);
		}
	}
}
