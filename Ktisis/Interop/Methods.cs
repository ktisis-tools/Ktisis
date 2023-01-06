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

		internal delegate IntPtr ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, ItemEquip item);
		internal static ChangeEquipDelegate? ActorChangeEquip;

		internal delegate void ChangeWeaponDelegate(IntPtr writeTo, int slot, WeaponEquip weapon, byte a4, byte a5, byte a6, byte a7); // a4-a7 is always 0,1,0,0.
		internal static ChangeWeaponDelegate? ActorChangeWeapon;

		// Get world matrix

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate WorldMatrix* GetMatrixDelegate();
		internal static GetMatrixDelegate? GetMatrix;

		// Status Effects
		internal delegate IntPtr AddStatusEffectDelegate(IntPtr writeTo, ushort statusId, ushort u1, IntPtr u2);
		internal static AddStatusEffectDelegate? StatusAddEffect;

		internal delegate IntPtr DeletedStatusEffectDelegate(IntPtr writeTo, int statusIndex, byte u2);
		internal static DeletedStatusEffectDelegate? StatusDeleteEffect;

		internal delegate ushort GetStatusEffectAtIndexDelegate(IntPtr writeTo, int statusIndex);
		internal static GetStatusEffectAtIndexDelegate? StatusGetEffect;

		// Animation
		public delegate IntPtr BlendActorAnimationDelegate(IntPtr writeTo, ushort actionId, IntPtr a1);
		internal static BlendActorAnimationDelegate? AnimationBlend;

		// Character Mode System
		public delegate IntPtr SetActorModeDelegate(IntPtr writeTo, ActorModes mode, byte modeInput);
		internal static SetActorModeDelegate? ActorSetMode;

		// Init & Dispose

		private static TDelegate Retrieve<TDelegate>(string sig)
			=> Marshal.GetDelegateForFunctionPointer<TDelegate>(Services.SigScanner.ScanText(sig));

		internal static void Init() {
			ActorLookAt = Retrieve<LookAtDelegate>("40 53 55 57 41 56 41 57 48 83 EC 70");
			ActorChangeEquip = Retrieve<ChangeEquipDelegate>("E8 ?? ?? ?? ?? 41 B5 01 FF C3");
			ActorChangeWeapon = Retrieve<ChangeWeaponDelegate>("E8 ?? ?? ?? ?? 80 7F 25 00");
			GetMatrix = Retrieve<GetMatrixDelegate>("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
			StatusAddEffect = Retrieve<AddStatusEffectDelegate>("66 85 D2 ?? ?? ?? ?? ?? ?? 48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 40 48 8B F1 49 8B");
			StatusDeleteEffect = Retrieve<DeletedStatusEffectDelegate>("83 FA 1E ?? ?? 53 48 83 EC 30 48 8B D9 8B C2 45 0F B6 D0 44 88 54 24 20 4C");
			StatusGetEffect = Retrieve<GetStatusEffectAtIndexDelegate>("4c 8b c1 83 fa 1e 72 03 33 c0 c3 8b c2 48 8d 0c 40 41 0f b7 44 88 08 c3");
			AnimationBlend = Retrieve<BlendActorAnimationDelegate>("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 0F B7 DA 48 8B F1 8B CB 49 8B F8 ?? ?? ?? ?? ?? 48 85 C0");
			ActorSetMode = Retrieve<SetActorModeDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 0F B6 B9 ?? ?? ?? ?? 41 8B E8");
		}
	}
}