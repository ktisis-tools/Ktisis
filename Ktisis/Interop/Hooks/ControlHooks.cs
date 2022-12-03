using System;

using Dalamud.Logging;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Events;
using Ktisis.Structs.Input;

namespace Ktisis.Interop.Hooks {
	internal static class ControlHooks {
		public static KeyboardState KeyboardState = new();

		internal unsafe delegate void InputDelegate(InputEvent* input, IntPtr a2, IntPtr a3, MouseState* mouseState, KeyboardState* keyState);
		internal static Hook<InputDelegate> InputHook = null!;

		internal unsafe static void InputDetour(InputEvent* input, IntPtr a2, IntPtr a3, MouseState* mouseState, KeyboardState* keyState) {
			InputHook.Original(input, a2, a3, mouseState, keyState);

			if (!Ktisis.IsInGPose) return;

			try {
				if (mouseState != null) {
					// TODO
				}

				// Process queue

				var keys = input->Keyboard->GetQueue();
				KeyboardState = *keys;

				for (var i = 0; i < keyState->QueueCount; i++) {
					var k = keyState->Queue[i];

					if (k->Event == KeyEvent.AnyKeyHeld) continue; // dont care didnt ask (use KeyEvent.Held)
					if (k->Event == KeyEvent.Released) continue; // Allow InputHook2 to take care of release events.

					if (EventManager.OnKeyPressed != null) {
						var invokeList = EventManager.OnKeyPressed.GetInvocationList();
						foreach (var invoke in invokeList) {
							var res = (bool)invoke.Method.Invoke(invoke.Target, new object[] { *k })!;
							if (res) {
								keys->KeyMap[k->KeyCode] = 0;
								keyState->KeyMap[k->KeyCode] = 0;
							}
						}
					}
				}
			} catch (Exception e) {
				PluginLog.Error(e, "Error in InputDetour.");
			}
		}

		// This function is pretty powerful. We only need it for the release event though; InputHook can't pick it up if an input gets blocked.
		// That said, this one can't reliably distinguish between a button being pressed or held, or give us the input state, so we need to use both.

		internal unsafe delegate IntPtr InputDelegate2(ulong a1, uint a2, ulong a3, uint a4);
		internal static Hook<InputDelegate2> InputHook2 = null!;

		internal unsafe static IntPtr InputDetour2(ulong a1, uint a2, ulong a3, uint a4) {
			var exec = InputHook2.Original(a1, a2, a3, a4);

			if (Ktisis.IsInGPose && a2 == 257) { // Released
				if (EventManager.OnKeyReleased != null)
					EventManager.OnKeyReleased((VirtualKey)a3);
			}

			return exec;
		}

		// Init & dispose

		internal static void Init() {
			unsafe {
				var addr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 83 7B 58 00");
				InputHook = Hook<InputDelegate>.FromAddress(addr, InputDetour);
				InputHook.Enable();

				var addr2 = Services.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 40 4D 8B F9");
				InputHook2 = Hook<InputDelegate2>.FromAddress(addr2, InputDetour2);
				InputHook2.Enable();
			}
		}

		internal static void Dispose() {
			InputHook.Disable();
			InputHook.Dispose();

			InputHook2.Disable();
			InputHook2.Dispose();
		}
	}
}