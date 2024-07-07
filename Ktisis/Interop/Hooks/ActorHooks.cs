using System;

using Dalamud.Hooking;

using Ktisis.Structs.Actor;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interop.Hooks {
	internal static class ActorHooks {
		// Control actor gaze
		// a1 = Actor + 0xC20

		internal delegate IntPtr ControlGazeDelegate(nint a1);
		internal static Hook<ControlGazeDelegate> ControlGazeHook = null!;

		internal unsafe static IntPtr ControlGaze(nint a1) {
			var actor = (Actor*)(a1 - Actor.GazeOffset);
			EditGaze.Apply(actor);
			return ControlGazeHook.Original(a1);
		}
		
		// Init & Dispose

		internal static void Init() {
			var controlGaze = Services.SigScanner.ScanText("40 57 41 54 41 56 48 81 EC ?? ?? ?? ?? 48 8B F9");
			ControlGazeHook = Services.Hooking.HookFromAddress<ControlGazeDelegate>(controlGaze, ControlGaze);
            ControlGazeHook.Enable();
		}

		internal static void Dispose() {
			ControlGazeHook.Disable();
			ControlGazeHook.Dispose();
		}
	}
}
