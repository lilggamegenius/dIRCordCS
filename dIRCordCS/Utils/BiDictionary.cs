using System;
using System.Collections.Generic;

namespace dIRCordCS.Utils{
	public class BiDictionary<TFirst, TSecond>{
		Dictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
		Dictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();

		public TSecond this[TFirst key]{
			get=>firstToSecond[key];
			set=>firstToSecond[key] = value;
		}

		public TFirst this[TSecond key]{
			get=>secondToFirst[key];
			set=>secondToFirst[key] = value;
		}

		public ICollection<TSecond> Values=>firstToSecond.Values;
		public ICollection<TFirst> Keys=>secondToFirst.Values;

		public void Add(TFirst first, TSecond second){
			if (firstToSecond.ContainsKey(first)){
				throw new ArgumentException("Duplicate first");
			}
			if (secondToFirst.ContainsKey(second)){
				throw new ArgumentException("Duplicate second");
			}
			firstToSecond.Add(first, second);
			secondToFirst.Add(second, first);
		}

		public bool TryGetByFirst(TFirst first, out TSecond second){
			return firstToSecond.TryGetValue(first, out second);
		}

		public bool TryGetBySecond(TSecond second, out TFirst first){
			return secondToFirst.TryGetValue(second, out first);
		}

		public Dictionary<TSecond, TFirst> Inverse(){
			return secondToFirst;
		}

		public static implicit operator Dictionary<TSecond, TFirst>(BiDictionary<TFirst, TSecond> obj){
			return obj.secondToFirst;
		}

		public static implicit operator Dictionary<TFirst, TSecond>(BiDictionary<TFirst, TSecond> obj){
			return obj.firstToSecond;
		}

		public static implicit operator BiDictionary<TFirst, TSecond>(Dictionary<TSecond, TFirst> obj){
			var ret = new BiDictionary<TFirst, TSecond>();
			foreach(TSecond key in obj.Keys){
				ret[key] = obj[key];
			}
			return ret;
		}

		public static implicit operator BiDictionary<TFirst, TSecond>(Dictionary<TFirst, TSecond> obj){
			var ret = new BiDictionary<TFirst, TSecond>();
			foreach(TFirst key in obj.Keys){
				ret[key] = obj[key];
			}
			return ret;
		}
	}
}
