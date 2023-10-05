using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Core;
using Ktisis.Interop;
using Ktisis.Core.Impl;
using Ktisis.Core.Services;
using Ktisis.Config;
using Ktisis.Config.Input;
using Ktisis.Input.Factory;
using Ktisis.Input.Hotkeys;

namespace Ktisis.Input;

[KtisisService]
public class InputService : IServiceInit {
	// Service
	
	private readonly InteropService _interop;
	private readonly IKeyState _keyState;
	private readonly GPoseService _gpose;
	private readonly ConfigService _cfg;
	private readonly IGameGui _gui;
	
	private HotkeyFactory Factory;

	private ControlHooks? ControlHooks;

	public InputService(
		IServiceContainer _services,
		InteropService _interop,
		IKeyState _keyState,
		GPoseService _gpose,
		ConfigService _cfg,
		IGameGui _gui
	) {
		this._interop = _interop;
		this._keyState = _keyState;
		this._gpose = _gpose;
		this._cfg = _cfg;
		this._gui = _gui;

		this.Factory = new HotkeyFactory(this, _services);
		
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}

	public void PreInit() {
		this.ControlHooks = this._interop.Create<ControlHooks>().Result;
		this.ControlHooks.OnKeyEvent += OnKeyEvent;
	}

	public void Initialize() {
		this.Factory.Create<GizmoHotkeys>()
			.Create<HistoryHotkeys>();
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

	public HotkeyInfo? GetActiveHotkey(VirtualKey key, HotkeyFlags flag) {
		HotkeyInfo? result = null;
		
		var modMax = 0;
		foreach (var (_, hk) in this.Hotkeys) {
			var bind = hk.Keybind;
			if (bind.Key != key || !hk.Flags.HasFlag(flag) || !bind.Mod.All(mod => this._keyState[mod]))
				continue;

			var modCt = bind.Mod.Length;
			if (result != null && modCt < modMax)
				continue;
			
			result = hk;
			modMax = modCt;
		}

		return result;
	}
	
	// Events

	private void OnGPoseUpdate(bool active) {
		if (active)
			this.ControlHooks?.EnableAll();
		else
			this.ControlHooks?.DisableAll();
	}

	private bool OnKeyEvent(VirtualKey key, VirtualKeyState state) {
		if (!this._cfg.Config.Keybinds_Active || !this._gpose.IsInGPose || IsChatInputActive())
			return false;

		var flag = state switch {
			VirtualKeyState.Down => HotkeyFlags.OnDown,
			VirtualKeyState.Held => HotkeyFlags.OnHeld,
			VirtualKeyState.Released => HotkeyFlags.OnRelease,
			_ => throw new Exception($"Invalid key state encountered ({state})")
		};

		var hk = GetActiveHotkey(key, flag);
		return hk?.Handler.Invoke(hk.Name) ?? false;
	}
	
	// Check chat state

	private unsafe bool IsChatInputActive() {
		var module = (UIModule*)this._gui.GetUIModule();
		if (module == null) return false;

		var atk = module->GetRaptureAtkModule();
		return atk != null && atk->AtkModule.IsTextInputActive();
	}
}
