using System;

namespace Ktisis.Helpers.Async {
	public class AsyncData<T> : AsyncTask {
		// Properties
		
		private T? Data;
		private readonly Func<object[], T> Callback;

		// Constructor
		
		public AsyncData(Func<object[], T> cb) : base(OnInvoke) {
			Callback = cb;
		}

		// Methods

		public T? Get(params object[] args) {
			if (State == TaskState.Waiting)
				Run(args);
			return Data;
		}
		
		public void Invalidate() {
			Data = default;
			State = TaskState.Waiting;
		}

		public T Consume() {
			var data = (T)(Data ?? default!);
			Invalidate();
			return data;
		}
		
		// AsyncTask Callback

		private static void OnInvoke(object self, params object[] args) {
			if (self is not AsyncData<T> task) return;
			task.Data = task.Callback(args);
		}
	}
}