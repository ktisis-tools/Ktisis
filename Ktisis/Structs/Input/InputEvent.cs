using System;

namespace Ktisis.Structs.Input {
	public unsafe struct InputEvent {
		public IntPtr Controller;
		public IntPtr UnknownDevice;
		public KeyboardDevice* Keyboard;
	}
}