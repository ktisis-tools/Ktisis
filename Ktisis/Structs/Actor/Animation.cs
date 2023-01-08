using System;
using System.Runtime.InteropServices;

using Ktisis.Interop;
using Ktisis.Interop.Hooks;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Animation {
		public const int AnimationSlotCount = 13;

		[FieldOffset(0x0E0)] public unsafe fixed ushort AnimationIds[AnimationSlotCount];

		[FieldOffset(0x154)] public unsafe fixed float Speeds[AnimationSlotCount];

		[FieldOffset(0x2B0)] public float OverallSpeed;

		[FieldOffset(0x2CC)] public ushort BaseOverride;

		[FieldOffset(0x2CE)] public ushort LipsOverride;

		public void SetBaseAnimation(ushort animationId, bool interrupt) {
			BaseOverride = animationId;

			if (interrupt)
				BlendAnimation(animationId);
		}

		public unsafe void BlendAnimation(ushort animationId) {
			if (Methods.AnimationBlend == null)
				return;

			fixed (Animation* p = &this) {
				IntPtr address = new IntPtr(p);
				Methods.AnimationBlend(address, animationId, IntPtr.Zero);
			}
		}

		public unsafe void RefreshAnimation() {
			AnimationIds[(int)AnimationSlots.Base] = 0; // Forces a refresh of all slots
		}

		public unsafe void SetSlotSpeed(AnimationSlots slotId, float speed) {
			fixed (Animation* p = &this) {
				IntPtr address = new IntPtr(p);
				ActorHooks.SetSlotSpeedHook.Original(address, (ushort)slotId, speed);
			}
		}
	}

	public enum AnimationSlots : int {
		Base = 0,
		UpperBody = 1,
		Facial = 2,
		Add = 3,
		Lips = 7,
		Parts1 = 8,
		Parts2 = 9,
		Parts3 = 10,
		Parts4 = 11,
		Overlay = 12
	}
}
