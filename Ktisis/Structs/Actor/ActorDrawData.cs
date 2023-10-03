using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorDrawData {
		[FieldOffset(0x010)] public Weapon MainHand;
		[FieldOffset(0x080)] public Weapon OffHand;
		[FieldOffset(0x0E8)] public Weapon Prop;
		
		[FieldOffset(0x160)] public Equipment Equipment;
		[FieldOffset(0x188)] public Customize Customize;
	}
}
