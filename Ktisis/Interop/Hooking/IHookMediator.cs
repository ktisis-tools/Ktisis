namespace Ktisis.Interop.Hooking;

public interface IHookMediator {
	public bool IsValid { get; }

	public T Create<T>(params object[] param) where T : HookModule;
	
	public bool Init(HookModule module);
	public bool Remove(HookModule module);
}
