using System;

namespace dIRCordCS.Utils{
	public class ResetException : Exception{
		public ResetException(){}
		public ResetException(string? message) : base(message){}
		public ResetException(string? message, Exception? innerException){}
	}
}
