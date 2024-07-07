using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorDrawData {
		[FieldOffset(0x010)] public Weapon MainHand;
		[FieldOffset(0x080)] public Weapon OffHand;
		[FieldOffset(0x0F0)] public Weapon Prop;
		
		[FieldOffset(0x160)] public Equipment Equipment;
		[FieldOffset(0x1B0)] public Customize Customize;

		[FieldOffset(0x1D0)] public ushort Glasses;
	}
}
