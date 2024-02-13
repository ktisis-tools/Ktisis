using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Structs.Attachment;

namespace Ktisis.Structs.Characters;

// Client::Graphics::Scene::CharacterBase

[StructLayout(LayoutKind.Explicit, Size = 0x8F0)]
public struct CharacterBaseEx {
	[FieldOffset(0x000)] public CharacterBase Base;

	[FieldOffset(0x050)] public Transform Transform;

	[FieldOffset(0x0D0)] public Attach Attach;

	[FieldOffset(0x2B0)] public WetnessState Wetness;

	[FieldOffset(0x8F0)] public CustomizeContainer Customize;

	//[FieldOffset(0x8F4)] public unsafe fixed uint DemiEquip[5];
	//[FieldOffset(0x910)] public unsafe fixed uint HumanEquip[10];
	[FieldOffset(0x910)] public EquipmentContainer Equipment;
}
