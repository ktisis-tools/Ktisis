using System;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Actors;

[Flags]
public enum CombatFlags : byte {
	None = 0,
	WeaponDrawn = 0x40
}

public enum EmoteModeEnum : uint {
	Normal = 0,
	SitGround = 1,
	SitChair = 2,
	Sleeping = 3
}

[StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
public struct CharacterEx {
	[FieldOffset(0)] public Character Character;

	[FieldOffset(0xE0)] public Vector3 DrawObjectOffset;

	[FieldOffset(0x130)] public Vector3 CameraOffsetSmooth;
	[FieldOffset(0x180)] public Vector3 CameraOffset;

	[FieldOffset(0x640)] public unsafe nint* _emoteControllerVf;
	[FieldOffset(0x640)] public EmoteController EmoteController;
	
	[FieldOffset(0x09B0)] public AnimationContainer Animation;
	
	[FieldOffset(0x0CF2)] public CombatFlags CombatFlags;

	[FieldOffset(0x21C8)] public float Opacity;

	[FieldOffset(0x226C)] public byte Mode;
	[FieldOffset(0x226D)] public EmoteModeEnum EmoteMode;
}
