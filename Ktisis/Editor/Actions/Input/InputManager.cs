using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Actions.Binds;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Actions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Hooking;

namespace Ktisis.Editor.Actions.Input;

public delegate bool KeyInvokeHandler();

public interface IInputManager : IDisposable {
	public void Initialize();

	public void Register(ActionKeybind keybind, KeyInvokeHandler handler, KeybindTrigger trigger);
}

public class InputManager : IInputManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	private readonly IKeyState _keyState;

	private Configuration Config => this._context.Config;
	
	private readonly List<KeybindRegister> Keybinds = new();
	
	public InputManager(
		IEditorContext context,
		HookScope scope,
		IKeyState keyState
	) {
		this._context = context;
		this._scope = scope;
		this._keyState = keyState;
	}
	
	// Initialization

	private InputModule? Module { get; set; }

	public void Initialize() {
		this.Module = this._scope.Create<InputModule>();
		this.Module.Initialize();
		this.Module.OnKeyEvent += this.OnKeyEvent;
		this.Module.EnableAll();
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
		if (!this._context.IsGPosing || !this.Config.Keybinds.Enabled || IsChatInputActive())
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
			if (!info.Trigger.HasFlag(trigger) || bind.Key != key || !bind.Modifiers.All(mod => this._keyState[mod]))
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

	public unsafe static bool IsChatInputActive() {
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
			this.Module?.Dispose();
			this.Keybinds.Clear();
			if (this.Module != null)
				this.Module.OnKeyEvent -= this.OnKeyEvent;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose input manager:\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
