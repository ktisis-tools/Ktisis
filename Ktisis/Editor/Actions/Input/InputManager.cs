using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Data.Config;
using Ktisis.Data.Config.Actions;
using Ktisis.Editor.Actions.Input.Binds;
using Ktisis.Editor.Context;

namespace Ktisis.Editor.Actions.Input;

public delegate bool KeyInvokeHandler();

public interface IInputManager : IDisposable {
	public void Initialize();

	public void Register(ActionKeybind keybind, KeyInvokeHandler handler, KeybindTrigger trigger);
}

public class InputManager : IInputManager {
	private readonly IContextMediator _mediator;
	private readonly InputModule _module;
	private readonly IKeyState _keyState;

	private Configuration Config => this._mediator.Config;
	
	private readonly List<KeybindRegister> Keybinds = new();
	
	public InputManager(
		IContextMediator mediator,
		InputModule module,
		IKeyState keyState
	) {
		this._mediator = mediator;
		this._module = module;
		this._keyState = keyState;
	}
	
	// Initialization

	public void Initialize() {
		this._module.Initialize();
		this._module.OnKeyEvent += this.OnKeyEvent;
		this._module.EnableAll();
	}
	
	// Keybinds

	public void Register(
		ActionKeybind keybind,
		KeyInvokeHandler handler,
		KeybindTrigger trigger
	) {
		var register = new KeybindRegister(keybind, handler, trigger);
		this.Keybinds.Add(register);
	}
	
	// Events
	
	private bool OnKeyEvent(VirtualKey key, VirtualKeyState state) {
		if (/*!this.Config.Keybinds.Enabled || */!this._mediator.IsGPosing || IsChatInputActive())
			return false;

		var flag = state switch {
			VirtualKeyState.Down => KeybindTrigger.OnDown,
			VirtualKeyState.Held => KeybindTrigger.OnHeld,
			VirtualKeyState.Released => KeybindTrigger.OnRelease,
			_ => throw new Exception($"Invalid key state encountered ({state})")
		};

		var hk = this.GetActiveHotkey(key, flag);
		return hk?.Handler.Invoke() ?? false;
	}
	
	private KeybindRegister? GetActiveHotkey(VirtualKey key, KeybindTrigger trigger) {
		KeybindRegister? result = null;
		
		var modMax = 0;
		foreach (var info in this.Keybinds) {
			var bind = info.Keybind.Combo;
			if (bind == null || !info.Trigger.HasFlag(trigger) || bind.Key != key || !bind.Modifiers.All(mod => this._keyState[mod]))
				continue;

			var modCt = bind.Modifiers.Length;
			if (result != null && modCt < modMax)
				continue;
			
			result = info;
			modMax = modCt;
		}

		return result;
	}
	
	// Check chat state

	private static unsafe bool IsChatInputActive() {
		var module = UIModule.Instance();
		if (module == null) return false;

		var atk = module->GetRaptureAtkModule();
		return atk != null && atk->AtkModule.IsTextInputActive();
	}
	
	// Registered

	private class KeybindRegister {
		public readonly ActionKeybind Keybind;
		public readonly KeyInvokeHandler Handler;
		public readonly KeybindTrigger Trigger;

		public KeybindRegister(
			ActionKeybind keybind,
			KeyInvokeHandler handler,
			KeybindTrigger trigger
		) {
			this.Keybind = keybind;
			this.Handler = handler;
			this.Trigger = trigger;
		}
		
		public bool Enabled => this.Keybind.Enabled;
	}
	
	// Disposal

	public void Dispose() {
		try {
			this.Keybinds.Clear();
			this._module.OnKeyEvent -= this.OnKeyEvent;
		} finally {
			this._module.Dispose();
		}
		GC.SuppressFinalize(this);
	}
}
