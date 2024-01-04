using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Hooking;

namespace Ktisis.Interop.Hooking;

public interface IHookModule : IDisposable {
	public bool IsInit { get; }

	public void EnableAll();
	public void DisableAll();
	public void SetEnabled(bool enabled);

	public bool TryGetHook<T>(out HookWrapper<T>? result) where T : Delegate;

	public bool Initialize();
}

public abstract class HookModule : IHookModule {
	private readonly IHookMediator _hook;

	private readonly List<IHookWrapper> Hooks = new();

	private bool _init;
	public bool IsInit => this._init && !this.IsDisposed;
	
	protected HookModule(
		IHookMediator hook
	) {
		this._hook = hook;
	}
	
	// Hook access

	public virtual void EnableAll() {
		this.Hooks.ForEach(hook => hook.Enable());
	}

	public virtual void DisableAll() {
		this.Hooks.ForEach(hook => hook.Disable());
	}
	
	public void SetEnabled(bool enabled) {
		if (enabled)
			this.EnableAll();
		else
			this.DisableAll();
	}

	public bool TryGetHook<T>(out HookWrapper<T>? result) where T : Delegate {
		result = null;
		foreach (var hook in this.Hooks) {
			if (hook is not HookWrapper<T> wrapper)
				continue;
			result = wrapper;
			return true;
		}
		return false;
	}
	
	// Hooking

	public virtual bool Initialize() {
		if (this.IsDisposed)
			throw new Exception("Attempted to initialize disposed module.");
		
		var init = this._init = this._hook.Init(this);

		var hooks = this.GetHookWrappers();
		if (init) {
			this.Hooks.AddRange(hooks);
		} else {
			foreach (var hook in hooks)
				hook.Dispose();
			this.IsDisposed = true;
		}
		
		return init;
	}

	private IEnumerable<IHookWrapper> GetHookWrappers() {
		var fields = this.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		foreach (var field in fields) {
			IHookWrapper? wrapper;
			
			try {
				wrapper = this.GetHookFromField(field);
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to resolve hook for field '{field.Name}':\n{err}");
				continue;
			}
			
			if (wrapper != null)
				yield return wrapper;
		}
	}

	private IHookWrapper? GetHookFromField(FieldInfo field) {
		var type = field.FieldType;
		if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Hook<>))
			return null;

		var value = field.GetValue(this);
		if (value == null) return null;

		var typeGen = typeof(HookWrapper<>)
			.GetGenericTypeDefinition()
			.MakeGenericType(type.GenericTypeArguments);

		return (IHookWrapper?)Activator.CreateInstance(typeGen, value);
	}
	
	// IDisposable

	private bool IsDisposed;

	public virtual void Dispose() {
		if (this.IsDisposed) return;
		try {
			this.Hooks.ForEach(hook => hook.Dispose());
			this.Hooks.Clear();
			this._hook.Remove(this);
		} finally {
			this.IsDisposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
