using System;

using Dalamud.Hooking;

namespace Ktisis.Interop.Hooking.Wrappers;

public interface IHookWrapper : IDalamudHook, IDisposable {
	public void Enable();
	public void Disable();

	public string GetName();
}

public class HookWrapper<T> : IHookWrapper where T : Delegate {
	private readonly Hook<T> _hook;

	public HookWrapper(Hook<T> _hook)
		=> this._hook = _hook;

	public nint Address => this._hook.Address;
	public bool IsEnabled => this._hook.IsEnabled;
	public bool IsDisposed => this._hook.IsDisposed;
	public string BackendName => this._hook.BackendName;

	public void Enable() => this._hook.Enable();
	public void Disable() => this._hook.Disable();
	
	public string GetName() => GetType().GetGenericArguments()[0].Name;
	
	public void Dispose() => this._hook.Dispose();
	
	public static HookWrapper<T> FromHook(Hook<T> hook) => new(hook);
}
