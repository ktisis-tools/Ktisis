using System;
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
		public const int Length = 159;
		
		public byte IsKeyPressed;
		public fixed uint KeyMap[Length];
		public KeyboardQueue Queue;
		public int KeyboardQueueCount;
		public int ControllerQueueCount;

		public bool IsKeyDown(VirtualKey key, bool consume = false) {
			var result = KeyMap[(int)key] != 0;
			if (result && consume) KeyMap[(int)key] = 0;
			return result;
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
