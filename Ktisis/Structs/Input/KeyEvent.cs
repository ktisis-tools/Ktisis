namespace Ktisis.Structs.Input;

public enum KeyEvent : byte {
	None = 0,
	Pressed = 1,
	Released = 2,
	AnyKeyHeld = 4,
	Held = 8
}
