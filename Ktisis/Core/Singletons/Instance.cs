using System;

namespace Ktisis.Core.Singletons;

internal class Instance {
	// Properties

	private readonly Type Type;
	protected Singleton? Singleton;

	private bool IsInit;
	private bool IsDisposed;

	internal string Name => Type.Name;

	// Ctor

	protected Instance(Type t) => Type = t;

	// Init & Dispose

	internal void Init() {
		if (IsInit) return;
		Singleton?.Init();
		IsInit = true;
	}

	internal void Dispose() {
		if (IsDisposed) return;
		Singleton?.Dispose();
		Singleton = null;
		IsDisposed = true;
	}
}

internal class Instance<T> : Instance where T : Singleton, new() {
	internal Instance() : base(typeof(T)) => Singleton = new T();
}
