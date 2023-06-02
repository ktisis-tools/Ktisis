using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Structs.Input {
	public unsafe struct KeyboardDevice {
		public KeyboardState* GetQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardState*>**)self)[0][4](self);
		}

		public KeyboardState* ClearQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardState*>**)self)[0][5](self);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct KeyboardState {
		public byte IsKeyPressed;
		public fixed uint KeyMap[159];
		public KeyboardQueue Queue;
		public int KeyboardQueueCount;
		public int ControllerQueueCount;

		public bool IsKeyDown(VirtualKey key)
			=> KeyMap[(int)key] == 1;
		public bool IsAnyOtherKeyDown(IEnumerable<VirtualKey> keys) {
			var keysInt = keys.Select(k=>(int)k);

			if (keys != null)
				for (int i = 0; i < 159; i++)
					if (KeyMap[i] == 1 && !keysInt.Contains(i))
						return true;
			return false;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct KeyboardQueue {
		public fixed Int64 _Raw[66];

		public QueueItem* this[int i] {
			get {
				fixed (Int64* ptr = _Raw)
					return (QueueItem*)((IntPtr)ptr + 4 + (8 * i));
			}
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct QueueItem {
		[FieldOffset(0)] public KeyEvent Event;
		[FieldOffset(1)] public byte KeyCode;
		[FieldOffset(4)] public byte Unknown;

		public VirtualKey VirtualKey => (VirtualKey)KeyCode;
	}
}
