using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorDrawData {
		[FieldOffset(0x010)] public Weapon MainHand;
		[FieldOffset(0x078)] public Weapon OffHand;
		[FieldOffset(0x0E0)] public Weapon Prop;
		
		[FieldOffset(0x148)] public Equipment Equipment;
		[FieldOffset(0x170)] public Customize Customize;
	}
}
