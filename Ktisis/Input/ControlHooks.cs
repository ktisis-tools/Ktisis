using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using Ktisis.Interop;
using Ktisis.Interop.Hooking;

namespace Ktisis.Input; 

public class ControlHooks : HookContainer {
	// Hooks

	[Signature("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 40 4D 8B F9")]
	private Hook<InputDelegate> InputHook = null!;
	
	private delegate nint InputDelegate(nint a1, WinMsg a2, nint a3, uint a4);

	private enum WinMsg : uint {
		WM_KEYDOWN = 0x100,
		WM_KEYUP = 0x101,
		WM_MOUSEMOVE = 0x200
	}

	private nint InputDetour(nint hWnd, WinMsg uMsg, nint wParam, uint lParam) {
		var exec = this.InputHook.Original(hWnd, uMsg, wParam, lParam);

		switch (uMsg) {
			case WinMsg.WM_KEYDOWN:
			case WinMsg.WM_KEYUP:
			case WinMsg.WM_MOUSEMOVE:
			default:
				break;
		}
		
		return exec;
	}
}
