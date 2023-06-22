using System;

namespace Ktisis.Core.Singletons;

public abstract class Singleton : IDisposable {
	public virtual void Init() { }
	public virtual void Dispose() { }
}

public abstract class Service : Singleton { }
