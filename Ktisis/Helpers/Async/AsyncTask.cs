using System;
using System.Threading.Tasks;

// Temporary solution for async troubles.

namespace Ktisis.Helpers.Async {
	public class AsyncTask {
		// Enums

		internal enum TaskState {
			Waiting,
			Running,
			Complete
		}

		// Properties
		
		private readonly object Callback;

		private Task? Task;
		internal TaskState State = TaskState.Waiting;

		// Constructor

		public AsyncTask(Action<object[]> cb)
			=> Callback = cb;
		
		public AsyncTask(Action<object, object[]> cb)
			=> Callback = cb;

		// Getters

		public bool IsWaiting => State == TaskState.Waiting;
		public bool IsRunning => State == TaskState.Running;
		public bool IsComplete => State == TaskState.Complete;

		// Methods

		public void Run(params object[] args) {
			if (State == TaskState.Running) return;
			
			State = TaskState.Running;
			Task = new Task(() => {
				try {
					if (Callback is Action<object[]> passArgs)
						passArgs.Invoke(args);
					else if (Callback is Action<object, object[]> passSelfArgs)
						passSelfArgs.Invoke(this, args);
				} finally {
					State = TaskState.Complete;
					//Task!.Dispose();
				}
			});
			Task.Start();
		}
	}
}