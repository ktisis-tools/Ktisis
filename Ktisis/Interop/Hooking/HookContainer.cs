using System;
using System.Collections.Generic;

using Dalamud.Hooking;

using Ktisis.Core;

namespace Ktisis.Interop.Hooking;

public abstract class HookContainer : INotifyReady {
	// Constructor

	private readonly InteropService _interop;

	public HookContainer(InteropService _interop) {
		this._interop = _interop;
	}

	public abstract void OnReady();

	// Hooks

	public bool Enabled { get; protected set; }

	protected readonly List<IHookWrapper> _hooks = new();

	public void EnableAll() {
		this.Enabled = true;
		this._hooks.ForEach(hook => hook.Enable());
	}

	public void DisableAll() {
		this.Enabled = false;
		this._hooks.ForEach(hook => hook.Disable());
	}

	protected Hook<T> AddSignature<T>(string sig, T detour) where T : Delegate {
		var hook = this._interop.Hooks.AddSignature(sig, detour);
		var wrapper = HookWrapper<T>.FromHook(hook);
		this._hooks.Add(wrapper.ToInterface());
		return hook;
	}
}
