

using System.Runtime.InteropServices;

namespace Ktisis.Structs.Input {

	//WIP: Don't try to use this unless you know what you're doing.
	[StructLayout(LayoutKind.Sequential)]
	public struct ControllerState {
		public int LeftPad1;
		public int LeftPad2;
		public int RightPad1;
		public int RightPad2;
		public ShapeState shapeState;
		public DirectionalPadState dpadState;

		public bool IsLeftStickUsed() {
			return LeftPad1 != 0 || LeftPad2 != 0;
		}

		public bool IsRightStickUsed() {
			return RightPad1 != 0 || RightPad2 != 0;
		}
	}

}
