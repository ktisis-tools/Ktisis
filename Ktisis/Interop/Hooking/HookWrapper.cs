using System;

using Dalamud.Hooking;

namespace Ktisis.Interop.Hooking;

public interface IHookWrapper : IDalamudHook, IDisposable {
	public string Name { get; }
	
	public void Enable();
	public void Disable();
}

public class HookWrapper<T> : IHookWrapper where T : Delegate {
	private readonly Hook<T> _hook;
	
	public string Name { get; }
	
	public HookWrapper(Hook<T> hook) {
		this._hook = hook;
		this.Name = this.GetType().GetGenericArguments()[0].Name;
	}

	public nint Address => this._hook.Address;
	public bool IsEnabled => this._hook.IsEnabled;
	public bool IsDisposed => this._hook.IsDisposed;
	public string BackendName => this._hook.BackendName;

	public void Enable() => this._hook.Enable();
	public void Disable() => this._hook.Disable();

	public void Dispose() {
		Ktisis.Log.Debug($"Disposing hook: '{this.Name}'");
		if (this._hook.IsEnabled)
			this._hook.Disable();
		this._hook.Dispose();
		GC.SuppressFinalize(this);
	}
}
