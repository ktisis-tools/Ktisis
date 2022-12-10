using System;

namespace Ktisis.Structs.Input {
	public unsafe struct InputEvent {
		public IntPtr Controller;
		public IntPtr UnknownDevice;
		public KeyboardDevice* Keyboard;
	}

	public enum KeyEvent : byte {
		None = 0,
		Pressed = 1,
		Released = 2,
		AnyKeyHeld = 4,
		Held = 8
	}
}
