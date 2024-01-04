using System;
using System.Linq;
using System.Threading.Tasks;

using Ktisis.Core.Attributes;

namespace Ktisis.Events; 

[Transient]
public class AsyncEvent<T> : EventBase<T> where T : Delegate {
	public Task InvokeAsync() => RunInvoke(
		sub => ((Func<Task>)sub).Invoke()
	);
	
	public Task InvokeAsync<T1>(T1 a1) => RunInvoke(
		sub => ((Func<T1, Task>)sub).Invoke(a1)
	);

	public Task InvokeAsync<T1, T2>(T1 a1, T2 a2) => RunInvoke(
		sub => ((Func<T1, T2, Task>)sub).Invoke(a1, a2)
	);

	public Task InvokeAsync<T1, T2, T3>(T1 a1, T2 a2, T3 a3) => RunInvoke(
		sub => ((Func<T1, T2, T3, Task>)sub).Invoke(a1, a2, a3)
	);
    
	private async Task RunInvoke(
		Func<object, Task> selector
	) {
		await Task.WhenAll(
			this._subscribers.Select(selector)
		).ContinueWith(LogException);
	}

	private void LogException(Task _task) {
		if (_task.Exception != null)
			Ktisis.Log.Error(_task.Exception.ToString());
	}
}
