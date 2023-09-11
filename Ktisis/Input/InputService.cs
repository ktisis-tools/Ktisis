using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Interop;
using Ktisis.Core.Impl;
using Ktisis.Core.Services;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Input;
using Ktisis.Input.Hotkeys;

namespace Ktisis.Input;

[KtisisService]
public class InputService : IServiceInit {
	// Service

	private readonly InteropService _interop;
	private readonly KeyState _keyState;
	private readonly GPoseService _gpose;
	private readonly ConfigService _cfg;
	private readonly IGameGui _gui;

	private ControlHooks? ControlHooks;

	public InputService(InteropService _interop, KeyState _keyState, GPoseService _gpose, ConfigService _cfg, IGameGui _gui) {
		this._interop = _interop;
		this._keyState = _keyState;
		this._gpose = _gpose;
		this._cfg = _cfg;
		this._gui = _gui;
		
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}

	public void PreInit() {
		this.ControlHooks = this._interop.Create<ControlHooks>().Result;
		this.ControlHooks.OnKeyEvent += OnKeyEvent;
	}
	
	// Hotkeys

	private readonly Dictionary<string, HotkeyInfo> Hotkeys = new();

	public void RegisterHotkey(HotkeyInfo hk, Keybind? defaultBind) {
		var cfg = this._cfg.Config;
		if (!cfg.Keybinds.TryGetValue(hk.Name, out var keybind) && defaultBind != null) {
			keybind = defaultBind;
			cfg.Keybinds.Add(hk.Name, keybind);
		}

		if (keybind != null)
			hk.Keybind = keybind;
		
		this.Hotkeys.Add(hk.Name, hk);
	}

	public IEnumerable<HotkeyInfo> GetActiveHotkeys(VirtualKey key) {
		foreach (var (_, hk) in this.Hotkeys) {
			var bind = hk.Keybind;
			if (bind.Key != key || !bind.Mod.All(mod => this._keyState[mod]))
				continue;
			
			yield return hk;
		}
	}
	
	// Events

	private void OnGPoseUpdate(bool active) {
		if (active)
			this.ControlHooks?.EnableAll();
		else
			this.ControlHooks?.DisableAll();
	}

	private bool OnKeyEvent(VirtualKey key, VirtualKeyState state) {
		if (!this._gpose.IsInGPose || IsChatInputActive())
			return false;

		var flag = state switch {
			VirtualKeyState.Down => HotkeyFlags.OnDown,
			VirtualKeyState.Held => HotkeyFlags.OnHeld,
			VirtualKeyState.Released => HotkeyFlags.OnRelease,
			_ => throw new Exception($"Invalid key state encountered ({state})")
		};
        
		return GetActiveHotkeys(key)
			.Where(hk => hk.Flags.HasFlag(flag))
			.Aggregate(false, (result, hk) => result | hk.Handler.Invoke(hk.Name));
	}
	
	// Check chat state

	private unsafe bool IsChatInputActive() {
		var module = (UIModule*)this._gui.GetUIModule();
		if (module == null) return false;

		var atk = module->GetRaptureAtkModule();
		return atk != null && atk->AtkModule.IsTextInputActive();
	}
}
