using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interop;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x84A)]
	public struct Actor {
		[FieldOffset(0)] public GameObject GameObject;

		[FieldOffset(0x88)] public byte ObjectID;

		[FieldOffset(0xF0)] public unsafe ActorModel* Model;
		[FieldOffset(0x1B4)] public int ModelId;

		[FieldOffset(0x818)] public Equipment Equipment;
		[FieldOffset(0x840)] public Customize Customize;

		[FieldOffset(0xC10 + 64)] public TrackPos LookAtHead;
		[FieldOffset(0xC10 + 64 + 480 * 2)] public TrackPos LookAtEyes;

		[FieldOffset(0x1A68)] public uint TargetObjectID;
		[FieldOffset(0x1A6C)] public byte TargetMode;

		public unsafe string? Name => Marshal.PtrToStringAnsi((IntPtr)GameObject.GetName());

		public unsafe IntPtr GetAddress() {
			fixed (Actor* self = &this) return (IntPtr)self;
		}

		// Targeting

		public unsafe void TargetActor(Actor* actor) {
			TargetObjectID = actor->ObjectID;
			TargetMode = 2;
		}

		public unsafe void LookAt(TrackPos* tar, int bodyPart) {
			if (ActorHooks.LookAt == null) return;

			ActorHooks.LookAt(
				GetAddress() + 0xC20,
				(IntPtr)tar,
				bodyPart,
				IntPtr.Zero
			);
		}

		// Change equipment - no redraw method

		public unsafe void Equip(EquipIndex index, EquipItem item) {
			if (ActorHooks.ChangeEquip == null) return;
			ActorHooks.ChangeEquip(GetAddress() + 0x6D0, index, item);
		}

		// Change customize - no redraw method

		public unsafe bool UpdateCustomize() {
			fixed (Customize* custom = &Customize)
				return ((Human*)Model)->UpdateDrawData((byte*)custom, true);
		}

		// Actor redraw

		public unsafe void Redraw() {
			GameObject.DisableDraw();
			GameObject.EnableDraw();
		}
	}
}