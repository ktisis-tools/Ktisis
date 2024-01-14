using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Structs.Input;

public struct KeyboardDeviceData {
	public const int Length = 160;

	public byte IsKeyPressed;
	public unsafe fixed uint KeyMap[Length];
	public KeyboardQueue Queue;
	public int KeyboardQueueCount;
	public int ControllerQueueCount;

	public unsafe bool IsKeyDown(VirtualKey key, bool consume = false) {
		var result = this.KeyMap[(int)key] != 0;
		if (result && consume)
			this.KeyMap[(int)key] = 0;
		return result;
	}
}

