using System;
using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Structs.Input;

namespace Ktisis.Interop.Hooks {
	internal static class ControlHooks {
		internal unsafe delegate void InputDelegate(InputEvent* input, IntPtr a2, IntPtr a3, MouseState* mouseState, KeyboardState* keyState);
		internal static Hook<InputDelegate> InputHook = null!;

		internal unsafe static void InputDetour(InputEvent* input, IntPtr a2, IntPtr a3, MouseState* mouseState, KeyboardState* keyState) {
			InputHook.Original(input, a2, a3, mouseState, keyState);

			if (!Ktisis.IsInGPose) return;

			if (mouseState != null) {
				// TODO
			}

			// TODO: Track released keys

			var keys = input->Keyboard->GetQueue();
			for (var i = 0; i < keyState->QueueCount; i++) {
				var k = keyState->Queue[i];

				if (k->Event == KeyEvent.AnyKeyHeld) continue; // dont care didnt ask (use KeyEvent.Held)

				if (EventManager.OnInputEvent != null) {
					var invokeList = EventManager.OnInputEvent.GetInvocationList();
					foreach (var invoke in invokeList) {
						var res = (bool)invoke.Method.Invoke(invoke.Target, new object[] { *k, *keys })!;
						if (res) {
							keys->KeyMap[k->KeyCode] = 0;
							keyState->KeyMap[k->KeyCode] = 0;
						}
					}
				}
			}
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