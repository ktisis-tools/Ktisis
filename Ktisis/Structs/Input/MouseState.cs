using System;
using System.Numerics;

namespace Ktisis.Structs.Input {
	public struct MouseState {
		public int PosX;
		public int PosY;
		public int ScrollDelta;
		public MouseButton Pressed;
		public MouseButton Clicked;
		public ulong Unk1;
		public int DeltaX;
		public int DeltaY;
		public uint Unk2;
		public bool IsFocused;
		
		public bool IsButtonHeld(MouseButton button) => (Pressed & button) != 0;

		public Vector2 GetDelta(bool consume = false) {
			var result = new Vector2(DeltaX, DeltaY);
			if (consume) {
				DeltaX = 0;
				DeltaY = 0;
			}
			return result;
		}
	}

	[Flags]
	public enum MouseButton {
		None = 0,
		Left = 1,
		Middle = 2,
		Right = 4,
		Mouse4 = 8,
		Mouse5 = 16
	}
}
