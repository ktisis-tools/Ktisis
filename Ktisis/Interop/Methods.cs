using System;
using System.Runtime.InteropServices;

using Ktisis.Services;
using Ktisis.Structs.Actor;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Interop
{
    internal class Methods {
		// Make actor look at co-ordinate point
		// a1 = Actor + 0xC20, a2 = TrackPos*, a3 = bodypart, a4 = ?

		internal unsafe delegate char LookAtDelegate(ActorGaze* writeTo, Gaze* readFrom, GazeControl bodyPart, IntPtr unk4);
		internal static LookAtDelegate ActorLookAt = null!;

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal delegate IntPtr ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, ItemEquip item);
		internal static ChangeEquipDelegate ActorChangeEquip = null!;

		internal delegate void ChangeWeaponDelegate(IntPtr writeTo, int slot, WeaponEquip weapon, byte a4, byte a5, byte a6, byte a7); // a4-a7 is always 0,1,0,0.
		internal static ChangeWeaponDelegate ActorChangeWeapon = null!;

		// Get world matrix

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate WorldMatrix* GetMatrixDelegate();
		internal static GetMatrixDelegate GetMatrix = null!;

		// Init & Dispose

		private static TDelegate Retrieve<TDelegate>(string sig)
			=> Marshal.GetDelegateForFunctionPointer<TDelegate>(DalamudServices.SigScanner.ScanText(sig));

		internal static void Init() {
			ActorLookAt = Retrieve<LookAtDelegate>("40 53 55 57 41 56 41 57 48 83 EC 70");
			ActorChangeEquip = Retrieve<ChangeEquipDelegate>("E8 ?? ?? ?? ?? 41 B5 01 FF C6");
			ActorChangeWeapon = Retrieve<ChangeWeaponDelegate>("E8 ?? ?? ?? ?? 80 7F 25 00");
			GetMatrix = Retrieve<GetMatrixDelegate>("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
		}
	}
}
