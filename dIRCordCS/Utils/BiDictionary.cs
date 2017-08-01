using System;
using System.Collections.Generic;

namespace dIRCordCS.Utils{
	public class BiDictionary<TFirst, TSecond>{
		IDictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
		IDictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();
		
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
			if (firstToSecond.ContainsKey(first) ||
			    secondToFirst.ContainsKey(second)){
				throw new ArgumentException("Duplicate first or second");
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
	}
}