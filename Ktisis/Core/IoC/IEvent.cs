using System;

namespace Ktisis.Core.IoC; 

public interface IEvent {
	public void Invoke();
	public void Invoke<T1>(T1 _sender);
	public void Invoke<T1, T2>(T1 _sender, T2 _args);
}

public interface IEvent<in D> : IEvent where D : Delegate {
	public IEventClient Subscribe(D handler);
	public void Unsubscribe(D handler);
}
