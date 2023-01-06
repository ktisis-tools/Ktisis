using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game;

using Ktisis.Interop;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public partial struct StatusEffects {
		public const int MAX_EFFECTS = 30;

		[FieldOffset(0)] StatusManager StatusManager;

		public unsafe void AddStatusEffect(ushort statusId) {
			fixed (void* p = &StatusManager) {
				Methods.StatusAddEffect?.Invoke(new IntPtr(p), statusId, 0, IntPtr.Zero);
			}
		}

		public unsafe void RemoveStatusEffect(int statusIndex) {
			fixed (void* p = &StatusManager) {
				Methods.StatusDeleteEffect?.Invoke(new IntPtr(p), statusIndex, 0);
			}
		}

		public unsafe ushort[] GetEffects() {
			ushort[] effects = new ushort[MAX_EFFECTS];

			if (Methods.StatusGetEffect != null) {
				fixed (void* p = &StatusManager) {
					IntPtr address = new IntPtr(p);

					for (int i = 0; i < MAX_EFFECTS; i++) {
						effects[i] = Methods.StatusGetEffect(address, i);
					}
				}
			}

			return effects;
		}

	}
}
