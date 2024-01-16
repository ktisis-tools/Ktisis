using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Structs.Characters;

// Client::Graphics::Scene::CharacterBase

[StructLayout(LayoutKind.Explicit, Size = 0x8F0)]
public struct CharacterEx {
	[FieldOffset(0x000)] public CharacterBase Base;

	[FieldOffset(0x0D0)] public Attach Attach;

	[FieldOffset(0x8F0)] public Customize Customize;

	[FieldOffset(0x8F4)] public unsafe fixed uint DemiEquip[5];
	[FieldOffset(0x910)] public unsafe fixed uint HumanEquip[10];

	public unsafe static CharacterEx* From(CharacterBase* cs) => (CharacterEx*)cs;
}
