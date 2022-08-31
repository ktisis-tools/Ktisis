using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x84A)]
	public unsafe struct Actor {
		[FieldOffset(0)] public GameObject GameObject;

		[FieldOffset(0x0F0)] public ActorModel* Model;
		[FieldOffset(0x840)] public Customize Customize;

		public string? Name {
			get => Marshal.PtrToStringAnsi((IntPtr)GameObject.GetName());
			set => throw new NotImplementedException();
		}

		// https://github.com/xivdev/Penumbra/blob/master/Penumbra/Interop/ObjectReloader.cs

		public static void DisableDraw(IntPtr addr)
		=> ((delegate* unmanaged<IntPtr, void>**)addr)[0][17](addr);

		public static void EnableDraw(IntPtr addr)
		=> ((delegate* unmanaged<IntPtr, void>**)addr)[0][16](addr);

		public unsafe void Redraw() {
			fixed (Actor* self = &this) {
				var ptr = (IntPtr)self;
				DisableDraw(ptr);
				EnableDraw(ptr);
			}
		}
	}
}