using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Structs.Input {
	public unsafe struct KeyboardDevice {
		public unsafe KeyboardState* GetQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardState*>**)self)[0][4](self);
		}

		public unsafe KeyboardState* ClearQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardState*>**)self)[0][5](self);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct KeyboardState {
		public byte IsKeyPressed;
		public fixed uint KeyMap[159];
		public KeyboardQueue Queue;
		public long QueueCount;

		public bool IsKeyDown(VirtualKey key)
			=> KeyMap[(int)key] == 1;
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
	public unsafe struct QueueItem {
		[FieldOffset(0)] public KeyEvent Event;
		[FieldOffset(1)] public byte KeyCode;
		[FieldOffset(4)] public byte Unknown;

		public VirtualKey VirtualKey => (VirtualKey)KeyCode;
	}
}