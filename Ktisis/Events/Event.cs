using System;

using Ktisis.Core.Attributes;

namespace Ktisis.Events; 

[Transient]
public class Event<T> : EventBase<T> where T : Delegate {
	private void Enumerate(
		Action<object> func
	) {
		foreach (var sub in this._subscribers) {
			try {
				func.Invoke(sub);
			} catch (Exception err) {
				Ktisis.Log.Error(err.ToString());
			}
		}
	}
	
	public void Invoke()
		=> Enumerate(sub => ((Action)sub).Invoke());
	
	public void Invoke<T1>(T1 a1)
		=> Enumerate(sub => ((Action<T1>)sub).Invoke(a1));

	public void Invoke<T1, T2>(T1 a1, T2 a2)
		=> Enumerate(sub => ((Action<T1, T2>)sub).Invoke(a1, a2));

	public void Invoke<T1, T2, T3>(T1 a1, T2 a2, T3 a3)
		=> Enumerate(sub => ((Action<T1, T2, T3>)sub).Invoke(a1, a2, a3));
}
