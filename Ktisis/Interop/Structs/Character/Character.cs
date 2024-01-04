using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Interop.Structs.Character;

// Client::Graphics::Scene::CharacterBase

[StructLayout(LayoutKind.Explicit, Size = 0x8F0)]
public struct Character {
	[FieldOffset(0x00)] public CharacterBase Base;

	[FieldOffset(0x8F0)] public Customize Customize;

	[FieldOffset(0x8F4)] public unsafe fixed uint DemiEquip[5];
	[FieldOffset(0x910)] public unsafe fixed uint HumanEquip[10];

	public static unsafe Character* From(CharacterBase* cs) => (Character*)cs;
}
