using System;
using System.Runtime.InteropServices;

using Ktisis.Structs.Actor;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Interop {
	internal class Methods {
		// Make actor look at co-ordinate point
		// a1 = Actor + 0xC20, a2 = TrackPos*, a3 = bodypart, a4 = ?

		internal unsafe delegate char LookAtDelegate(ActorGaze* writeTo, Gaze* readFrom, GazeControl bodyPart, IntPtr unk4);
		internal static LookAtDelegate? ActorLookAt;

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal unsafe delegate void ChangeEquipDelegate(ActorDrawData* writeTo, EquipIndex index, ItemEquip* item, bool force);
		internal static ChangeEquipDelegate? ActorChangeEquip;

		internal unsafe delegate void ChangeWeaponDelegate(ActorDrawData* writeTo, int slot, WeaponEquip weapon, byte a4, byte a5, byte a6, byte a7); // a4-a7 is always 0,1,0,0.
		internal static ChangeWeaponDelegate? ActorChangeWeapon;

		internal unsafe delegate void ChangeGlassesDelegate(ActorDrawData* writeTo, int slot, ushort id);
		internal static ChangeGlassesDelegate? ChangeGlasses;

		// Get world matrix

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate WorldMatrix* GetMatrixDelegate();
		internal static GetMatrixDelegate? GetMatrix;

		// Init & Dispose

		private static TDelegate Retrieve<TDelegate>(string sig)
			=> Marshal.GetDelegateForFunctionPointer<TDelegate>(Services.SigScanner.ScanText(sig));

		internal static void Init() {
			ActorLookAt = Retrieve<LookAtDelegate>("E8 ?? ?? ?? ?? 8B D6 48 8B CF E8 ?? ?? ?? ?? EB 2A");
			ActorChangeEquip = Retrieve<ChangeEquipDelegate>("E8 ?? ?? ?? ?? B1 01 41 FF C6");
			ActorChangeWeapon = Retrieve<ChangeWeaponDelegate>("E8 ?? ?? ?? ?? 4C 8B 45 7F");
			ChangeGlasses = Retrieve<ChangeGlassesDelegate>("E8 ?? ?? ?? ?? EB 50 44 8B 03");
			GetMatrix = Retrieve<GetMatrixDelegate>("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
		}
	}
}
