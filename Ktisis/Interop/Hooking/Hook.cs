using System;

using Dalamud.Hooking;
using Dalamud.Logging;

namespace Ktisis.Interop.Hooking;

public abstract class FuncHook : IDisposable {
	public abstract void Enable();
	public abstract void Disable();
	public abstract void Dispose();
}

public class FuncHook<T> : FuncHook where T : Delegate {
	private readonly Hook<T> Hook;

	public FuncHook(nint address, T detour)
		=> Hook = Hook<T>.FromAddress(address, detour);

	public T Original => Hook.Original;

	public override void Enable() => Hook.Enable();
	public override void Disable() => Hook.Disable();
	public override void Dispose() {
		Hook.Disable();
		Hook.Dispose();
	}
}
