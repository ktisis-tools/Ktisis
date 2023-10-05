using System;
using System.Collections.Generic;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

using Ktisis.Interop.Hooking.Wrappers;

namespace Ktisis.Interop.Hooking; 

public class HookManager : IDisposable {
	// Constructor

	private readonly ISigScanner _sig;
	private readonly IGameInteropProvider _interop;
	
	public HookManager(ISigScanner _sig, IGameInteropProvider _interop) {
		this._sig = _sig;
		this._interop = _interop;
	}
	
	// Hook registration & creation
	
	private readonly List<IHookWrapper> Registered = new();

	public void Add(IHookWrapper hook) {
		this.Registered.Add(hook);
		Ktisis.Log.Verbose($"Registered hook: {hook.GetName()} @ 0x{hook.Address:X}");
	}

	public void Add<T>(Hook<T> hook) where T : Delegate {
		var inst = HookWrapper<T>.FromHook(hook);
		Add(inst);
		Ktisis.Log.Verbose($"Registered hook: {inst.GetName()} @ 0x{hook.Address:X}");
	}
	
	public Hook<T> AddAddress<T>(nint addr, T detour) where T : Delegate {
		var hook = this._interop.HookFromAddress(addr, detour);
		Add(hook);
		return hook;
	}

	public Hook<T> AddSignature<T>(string sig, T detour) where T : Delegate {
		var addr = this._sig.ScanText(sig);
		return AddAddress(addr, detour);
	}
	
	// Helpers

	private static string GetHookName<T>(Hook<T> hook) where T : Delegate
		=> hook.GetType().GetGenericArguments()[0].Name;
	
	// Disposal

	public void Dispose() {
		this.Registered.ForEach(Dispose);
	}

	private void Dispose(IHookWrapper hook) {
		var name = hook.GetName();
		try {
			hook.Disable();
			if (!hook.IsDisposed)
				hook.Dispose();
			Ktisis.Log.Verbose($"Disposed hook: {name}");
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose {name}':\n{err}");
		}
	}
}
