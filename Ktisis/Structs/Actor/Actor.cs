using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x84A)]
	public unsafe struct Actor {
		[FieldOffset(0)] public GameObject GameObject;

		[FieldOffset(0x0F0)] public ActorModel* Model;
		[FieldOffset(0x830)] public Customize Customize;
	}
}