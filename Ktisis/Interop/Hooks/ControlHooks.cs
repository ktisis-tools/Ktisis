using System;

using Dalamud.Hooking;

using Ktisis.Overlay;
using Ktisis.Structs.Input;

namespace Ktisis.Interop.Hooks {
	internal static class ControlHooks {
		internal unsafe delegate void InputDelegate(InputEvent* keyState, IntPtr a2, IntPtr a3, MouseState* mouseState, IntPtr a5);
		internal static Hook<InputDelegate> InputHook = null!;

		internal unsafe static void InputDetour(InputEvent* keyState, IntPtr a2, IntPtr a3, MouseState* mouseState, IntPtr a5) {
			if (mouseState != null) {
				// TODO
			}

			if (keyState != null) {
				var keys = keyState->Keyboard->GetQueue();
				for (var i = 0; i < keys->QueueCount; i++) {
					var k = keys->Queue[i];

					// TODO: Input event manager
					if (k->Event == KeyEvent.Pressed && k->KeyCode == 27) {
						if (OverlayWindow.GizmoOwner != null) {
							OverlayWindow.SetGizmoOwner(null);
							k->Event = KeyEvent.None;
							keyState->Keyboard->ClearQueue();
						}
					}
				}
			}

			InputHook.Original(keyState, a2, a3, mouseState, a5);
		}

		// Init & dispose

		internal static void Init() {
			unsafe {
				var addr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 83 7B 58 00");
				InputHook = Hook<InputDelegate>.FromAddress(addr, InputDetour);
				InputHook.Enable();
			}
		}

		internal static void Dispose() {
			InputHook.Disable();
			InputHook.Dispose();
		}
	}
}