using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;

using Ktisis.Structs.Actor;

namespace Ktisis.Interop {
	public class ActorHooks {
		// Make actor look at co-ordinate point
		// a1 = Actor + 0xC20, a2 = TrackPos*, a3 = bodypart, a4 = ?

		internal unsafe delegate char LookAtDelegate(ActorGaze* writeTo, Gaze* readFrom, GazeControl bodyPart, IntPtr unk4);
		internal static LookAtDelegate? LookAt;

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal delegate IntPtr ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, EquipItem item);
		internal static ChangeEquipDelegate? ChangeEquip;

		// Control actor gaze
		// a1 = Actor + 0xC20

		internal delegate IntPtr ControlGazeDelegate(IntPtr a1);
		internal static Hook<ControlGazeDelegate> ControlGazeHook = null!;

		internal unsafe static IntPtr ControlGaze(IntPtr a1) {
			var actor = (Actor*)(a1 - 0xC10);
			return ControlGazeHook.Original(a1);
		}

		// Init & Dispose

		internal static void Init() {
			var lookAt = Dalamud.SigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 70");
			LookAt = Marshal.GetDelegateForFunctionPointer<LookAtDelegate>(lookAt);

			var changeEquip = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B5 01 FF C3");
			ChangeEquip = Marshal.GetDelegateForFunctionPointer<ChangeEquipDelegate>(changeEquip);

			var controlGaze = Dalamud.SigScanner.ScanText("40 53 41 54 41 55 48 81 EC ?? ?? ?? ?? 48 8B D9");
			ControlGazeHook = Hook<ControlGazeDelegate>.FromAddress(controlGaze, ControlGaze);
			ControlGazeHook.Enable();
		}

		internal static void Dispose() {
			LookAt = null;
			ChangeEquip = null;

			ControlGazeHook.Disable();
			ControlGazeHook.Dispose();
		}
	}
}
