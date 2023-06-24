namespace Ktisis.Core.Singletons;

public abstract class Service {
	public virtual void Init() { }
	public virtual void Dispose() { }
	public virtual void OnReady() { }
}
