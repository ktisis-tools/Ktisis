using System.Linq;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Interop.Hooking;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Editor.Actions.Input;

public enum VirtualKeyState {
	Down,
	Held,
	Released
}

public delegate bool KeyEventHandler(VirtualKey key, VirtualKeyState state);

public class InputModule : HookModule {
	private IEditorContext _context;
	public InputModule(
		IHookMediator hook,
		IEditorContext context
	) : base(hook) {
		this._context = context;
	}
	
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

	[Signature("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 40 4D 8B F9", DetourName = nameof(InputNotificationDetour))]
	private Hook<InputNotificationDelegate> InputNotificationHook = null!;
	private delegate nint InputNotificationDelegate(nint a1, WinMsg a2, nint a3, uint a4);

	private nint InputNotificationDetour(nint hWnd, WinMsg uMsg, nint wParam, uint lParam) {
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
		
		return this.InputNotificationHook.Original(hWnd, uMsg, wParam, lParam);
	}

	[Signature("E8 ?? ?? ?? ?? 4C 8B BC 24 ?? ?? ?? ?? 4C 8B B4 24 ?? ?? ?? ?? 48 8B B4 24 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", DetourName = nameof(ProcessMouseStateDetour))]
	private Hook<ProcessMouseStateDelegate> ProcessMouseStateHook = null!;
	private unsafe delegate nint ProcessMouseStateDelegate(TargetSystem* targets, nint a2, nint a3);

	private unsafe nint ProcessMouseStateDetour(TargetSystem* targets, nint a2, nint a3) {
		var prev = targets->GPoseTarget;
		nint exec = this.ProcessMouseStateHook!.Original(targets, a2, a3);

		if (targets->GPoseTarget != prev) {
			var leftBlocked = this._context.Config.Keybinds.BlockTargetLeftClick && exec == 0;
			var rightBlocked = !leftBlocked && this._context.Config.Keybinds.BlockTargetRightClick && exec == 0x10;

			// if config determined we should block either from a left click or right click, revert the targeting done in exec
			if (leftBlocked || rightBlocked) targets->GPoseTarget = prev;
		}

		return exec;
	}
}
