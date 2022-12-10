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
	}

	public enum MouseButton {
		None = 0,
		Left = 1,
		Middle = 2,
		Right = 4,
		Mouse4 = 8,
		Mouse5 = 16
	}
}
