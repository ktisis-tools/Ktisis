using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Structs.Input;

[StructLayout(LayoutKind.Sequential)]
public struct KeyboardQueue {
	public unsafe fixed ulong _data[66];

	public unsafe QueueEntry this[int i] {
		get {
			fixed (ulong* ptr = this._data)
				return ((QueueEntry*)ptr)[i];
		}
	}
}

[StructLayout(LayoutKind.Explicit)]
public struct QueueEntry {
	[FieldOffset(0)] public KeyEvent Event;
	[FieldOffset(1)] public byte KeyCode;
	[FieldOffset(4)] public byte Unknown;

	public VirtualKey VirtualKey => (VirtualKey)this.KeyCode;
}