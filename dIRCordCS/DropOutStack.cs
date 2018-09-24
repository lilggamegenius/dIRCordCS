namespace dIRCordCS{
	public class DropOutStack<T>{
		private readonly T[] items;
		private int top;
		public DropOutStack(int capacity)=>items = new T[capacity];

		public void Push(T item){
			items[top++] = item;
			top %= items.Length;
		}
		public T Pop(){
			top = ((items.Length + top) - 1) % items.Length;
			return items[top];
		}

		public T Peek()=>items[top];
	}
}
