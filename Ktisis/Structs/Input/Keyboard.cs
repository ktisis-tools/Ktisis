using System;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Input {
	public unsafe struct KeyboardDevice {
		public unsafe KeyboardInput* GetQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardInput*>**)self)[0][4](self);
		}

		public unsafe KeyboardInput* ClearQueue() {
			fixed (KeyboardDevice* self = &this)
				return ((delegate* unmanaged<KeyboardDevice*, KeyboardInput*>**)self)[0][5](self);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct KeyboardInput {
		public byte IsKeyPressed;
		public fixed uint KeyMap[159];
		public KeyboardQueue Queue;
		public byte Padding1;
		public long QueueCount;

		public bool IsKeyDown(byte code)
			=> KeyMap[code] == 1;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct KeyboardQueue {
		public fixed Int64 _Raw[65];

		public QueueItem* this[int i] {
			get {
				fixed (Int64* ptr = _Raw)
					return (QueueItem*)((IntPtr)ptr + 4 + (8 * i));
			}
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct QueueItem {
		[FieldOffset(0)] public byte Event;
		[FieldOffset(1)] public byte KeyCode;
		[FieldOffset(4)] public byte Unknown;
	}
}