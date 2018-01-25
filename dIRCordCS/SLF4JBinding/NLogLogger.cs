using System;
using NLog;
using org.slf4j.helpers;

namespace dIRCordCS.SLF4JBinding{
	public class NLogLogger : MarkerIgnoringBase{
		private readonly Logger Logger;

		public NLogLogger(string className){
			Logger = LogManager.GetLogger(className);
		}

		#region Trace

		public override bool isTraceEnabled(){
			return Logger.IsTraceEnabled;
		}
		public override void trace(string obj0){
			Logger.Trace(obj0);
		}
		public override void trace(string obj0, object[] obj1){
			Logger.Trace(obj0, obj1);
		}
		public override void trace(string obj0, object obj1){
			Logger.Trace(obj0, obj1);
		}
		public override void trace(string obj0, object obj1, object obj2){
			Logger.Trace(obj0, obj1, obj2);
		}
		public override void trace(string obj0, Exception obj1){
			Logger.Trace(obj1, obj0);
		}

		#endregion

		#region Debug

		public override bool isDebugEnabled(){
			return Logger.IsDebugEnabled;
		}
		public override void debug(string obj0){
			Logger.Debug(obj0);
		}
		public override void debug(string obj0, object[] obj1){
			Logger.Debug(obj0, obj1);
		}
		public override void debug(string obj0, object obj1){
			Logger.Debug(obj0, obj1);
		}
		public override void debug(string obj0, object obj1, object obj2){
			Logger.Debug(obj0, obj1, obj2);
		}
		public override void debug(string obj0, Exception obj1){
			Logger.Debug(obj1, obj0);
		}

		#endregion

		#region Info

		public override bool isInfoEnabled(){
			return Logger.IsInfoEnabled;
		}
		public override void info(string obj0){
			Logger.Info(obj0);
		}
		public override void info(string obj0, object[] obj1){
			Logger.Info(obj0, obj1);
		}
		public override void info(string obj0, object obj1){
			Logger.Info(obj0, obj1);
		}
		public override void info(string obj0, object obj1, object obj2){
			Logger.Info(obj0, obj1, obj2);
		}
		public override void info(string obj0, Exception obj1){
			Logger.Info(obj1, obj0);
		}

		#endregion

		#region Warn

		public override bool isWarnEnabled(){
			return Logger.IsWarnEnabled;
		}
		public override void warn(string obj0){
			Logger.Warn(obj0);
		}
		public override void warn(string obj0, object[] obj1){
			Logger.Warn(obj0, obj1);
		}
		public override void warn(string obj0, object obj1){
			Logger.Warn(obj0, obj1);
		}
		public override void warn(string obj0, object obj1, object obj2){
			Logger.Warn(obj0, obj1, obj2);
		}
		public override void warn(string obj0, Exception obj1){
			Logger.Warn(obj1, obj0);
		}

		#endregion

		#region Error

		public override bool isErrorEnabled(){
			return Logger.IsErrorEnabled;
		}
		public override void error(string obj0){
			Logger.Error(obj0);
		}
		public override void error(string obj0, object[] obj1){
			Logger.Error(obj0, obj1);
		}
		public override void error(string obj0, object obj1){
			Logger.Error(obj0, obj1);
		}
		public override void error(string obj0, object obj1, object obj2){
			Logger.Error(obj0, obj1, obj2);
		}
		public override void error(string obj0, Exception obj1){
			Logger.Error(obj1, obj0);
		}

		#endregion
	}
}
