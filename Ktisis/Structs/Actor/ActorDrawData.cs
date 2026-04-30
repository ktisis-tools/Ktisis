using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorDrawData {
		[FieldOffset(0x010)] public Weapon MainHand;
		[FieldOffset(0x080)] public Weapon OffHand;
		[FieldOffset(0x0F0)] public Weapon Prop;
		
		[FieldOffset(0x1D0)] public Equipment Equipment;
		[FieldOffset(0x220)] public Customize Customize;

		[FieldOffset(0x240)] public ushort Glasses;
	}
}
