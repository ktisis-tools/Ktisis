using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Ktisis.Structs.Characters;

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct AttachParam {
	[FieldOffset(0x00)] public ushort ChildBoneId;
	[FieldOffset(0x02)] public ushort ParentBoneId;
	[FieldOffset(0x10)] public Transform Transform;
}
