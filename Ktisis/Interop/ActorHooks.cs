using System;
using System.Runtime.InteropServices;

using Ktisis.Structs.Actor;

namespace Ktisis.Interop {
	public class ActorHooks {
		// Make actor look at co-ordinate point
		// a1 = Actor + 0xC10, a2 = TrackPos*, a3 = bodypart, a4 = ?

		internal delegate char LookAtDelegate(IntPtr writeTo, IntPtr readFrom, int bodyPart, IntPtr unk4);
		internal static LookAtDelegate? LookAt;

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal delegate IntPtr ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, EquipItem item);
		internal static ChangeEquipDelegate? ChangeEquip;

		// Init & Dispose

		internal static void Init() {
			var lookAt = Dalamud.SigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 70");
			LookAt = Marshal.GetDelegateForFunctionPointer<LookAtDelegate>(lookAt);

			var changeEquip = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B5 01 FF C3");
			ChangeEquip = Marshal.GetDelegateForFunctionPointer<ChangeEquipDelegate>(changeEquip);
		}

		internal static void Dispose() {
			LookAt = null;
			ChangeEquip = null;
		}
	}
}
