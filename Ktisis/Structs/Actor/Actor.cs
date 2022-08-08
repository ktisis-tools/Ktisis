using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0xF8)]
	public unsafe struct Actor {
		[FieldOffset(0)] public GameObject GameObject;

		[FieldOffset(0xF0)] public ActorModel* Model;
	}
}