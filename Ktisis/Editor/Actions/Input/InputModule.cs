using System.Linq;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Interop.Hooking;

namespace Ktisis.Editor.Actions.Input;

public enum VirtualKeyState {
	Down,
	Held,
	Released
}

public delegate bool KeyEventHandler(VirtualKey key, VirtualKeyState state);

public class InputModule : HookModule {

	public InputModule(
		IHookMediator hook
	) : base(hook) { }
	
	// Events

	public event KeyEventHandler? OnKeyEvent;

	private bool InvokeKeyEvent(VirtualKey key, VirtualKeyState state) {
		if (this.OnKeyEvent == null) return false;
		return this.OnKeyEvent.GetInvocationList()
			.Cast<KeyEventHandler>()
			.Aggregate(false, (result, handler) => result | handler.Invoke(key, state));
	}
	
	// Data
	
	private enum WinMsg : uint {
		WM_KEYDOWN = 0x100,
		WM_KEYUP = 0x101,
		WM_MOUSEMOVE = 0x200
	}
	
	// Hooks

	[Signature("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 40 4D 8B F9", DetourName = nameof(InputDetour))]
	private Hook<InputDelegate> InputHook = null!;
	
	private delegate nint InputDelegate(nint a1, WinMsg a2, nint a3, uint a4);

	private nint InputDetour(nint hWnd, WinMsg uMsg, nint wParam, uint lParam) {
		var key = (VirtualKey)wParam;
		switch (uMsg) {
			// https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
			case WinMsg.WM_KEYDOWN:
				if (this.InvokeKeyEvent(key, (lParam >> 30) != 0 ? VirtualKeyState.Held : VirtualKeyState.Down))
					return 0;
				break;
			case WinMsg.WM_KEYUP:
				if (this.InvokeKeyEvent(key, VirtualKeyState.Released))
					return 0;
				break;
			case WinMsg.WM_MOUSEMOVE:
			default:
				break;
		}
		
		return this.InputHook.Original(hWnd, uMsg, wParam, lParam);
	}
}
