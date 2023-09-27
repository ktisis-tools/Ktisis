using System;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

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

		internal static Hook<UpdateCustomizeDelegate> UpdateCustomizeHook = null!;
		internal unsafe delegate bool UpdateCustomizeDelegate(ActorModel* self, Customize* custom, bool skipEquip);
		internal unsafe static bool UpdateCustomizeDetour(ActorModel* self, Customize* custom, bool skipEquip) {
			var exec = UpdateCustomizeHook.Original(self, custom, skipEquip);
			if (!Ktisis.IsInGPose || !skipEquip) return exec;

			var actors = Services.ObjectTable;
				//.Where(x => x.Address != nint.Zero && x.ObjectIndex is >= 200 and < 240);
			
			foreach (var actor in actors) {
				var csActor = (GameObject*)actor.Address;
				PluginLog.Information($"{actor.ObjectIndex} {(nint)csActor->DrawObject} == {(nint)self:X}");
				if ((ActorModel*)csActor->DrawObject != self) continue;
				PluginLog.Information($"Normalizing");
				//Methods.NormalizeCustomize?.Invoke(&self->Customize, custom);
			}
			
			return exec;
		}

		// Init & Dispose

		internal unsafe static void Init() {
			var controlGaze = Services.SigScanner.ScanText("40 53 41 54 41 55 48 81 EC ?? ?? ?? ?? 48 8B D9");
			ControlGazeHook = Hook<ControlGazeDelegate>.FromAddress(controlGaze, ControlGaze);
			ControlGazeHook.Enable();

			var updateCustom = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 83 BF ?? ?? ?? ?? ?? 75 34");
			UpdateCustomizeHook = Hook<UpdateCustomizeDelegate>.FromAddress(updateCustom, UpdateCustomizeDetour);
			//UpdateCustomizeHook.Enable();
		}

		internal static void Dispose() {
			ControlGazeHook.Disable();
			ControlGazeHook.Dispose();
			
			UpdateCustomizeHook.Disable();
			UpdateCustomizeHook.Dispose();
		}
	}
}
