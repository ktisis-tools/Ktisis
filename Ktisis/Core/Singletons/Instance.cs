using System;

using Ktisis.Events;

namespace Ktisis.Core.Singletons;

internal class Instance {
	// Properties
	
	private readonly Type Type;
	public Singleton? Singleton { get; protected set; }

	private bool IsInit;
	private bool IsDisposed;

	internal string Name => Type.Name;
	
	// Ctor

	protected Instance(Type t) => Type = t;
	
	// Wrappers

	internal void Init() {
		if (IsInit) return;
		Singleton?.Init();
		if (Singleton is IEventClient eventClient)
			Services.Events.Create(eventClient);
		IsInit = true;
	}

	internal void OnReady() {
		if (!IsInit) return;
		Singleton?.OnReady();
	}

	internal void Dispose() {
		if (IsDisposed) return;
		if (Singleton is IEventClient eventClient)
			Services.Events.Remove(eventClient);
		Singleton?.Dispose();
		Singleton = null;
		IsDisposed = true;
	}
}

internal class Instance<T> : Instance where T : Singleton, new() {
	internal Instance() : base(typeof(T)) => Singleton = new T();
}