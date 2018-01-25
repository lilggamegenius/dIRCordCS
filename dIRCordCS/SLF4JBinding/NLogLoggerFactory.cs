using org.slf4j;

namespace dIRCordCS.SLF4JBinding{
	public class NLogLoggerFactory : ILoggerFactory{
		public Logger getLogger(string str){
			return new NLogLogger(str);
		}
	}
}
