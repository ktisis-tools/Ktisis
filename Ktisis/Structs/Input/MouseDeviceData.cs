using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Input;

[Flags]
public enum MouseButton {
	None = 0,
	Left = 1,
	Middle = 2,
	Right = 4,
	Mouse4 = 8,
	Mouse5 = 16
}

[StructLayout(LayoutKind.Sequential)]
public struct MouseDeviceData {
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
		
	public bool IsButtonHeld(MouseButton button) => (this.Pressed & button) != 0;

	public Vector2 GetDelta(bool consume = false) {
		var result = new Vector2(this.DeltaX, this.DeltaY);
		if (consume) {
			this.DeltaX = 0;
			this.DeltaY = 0;
		}
		return result;
	}
}
