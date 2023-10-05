using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

using Ktisis.Core;
using Ktisis.Interop.Structs.Event;
using Ktisis.Common.Extensions;
using Ktisis.Events;

namespace Ktisis.Services;

public delegate void GPoseUpdate(bool active);

[DIService]
public class GPoseService : IDisposable {
	// Service

	private readonly IObjectTable _actors;
	private readonly IFramework _framework;
	private readonly IClientState _state;
	private readonly IGameInteropProvider _interop;

	public GPoseService(
		IObjectTable _actors,
		IFramework _framework,
		IClientState _state,
		IGameInteropProvider _interop,
		InitEvent _init,
		InitHooksEvent _initHooks
	) {
		this._actors = _actors;
		this._framework = _framework;
		this._state = _state;
		this._interop = _interop;

		_init.Subscribe(Initialize);
		_initHooks.Subscribe(InitHooks);
	}
	
	private void Initialize() {
		this._framework.Update += OnFrameworkEvent;
	}
	
	private void InitHooks() {
		this._interop.InitializeFromAttributes(this);
	}
	
	// State
	
	public bool IsInGPose { get; private set; }
	
	// Events

	public event GPoseUpdate? OnGPoseUpdate;

	private void OnFrameworkEvent(object _sender) {
		if (this.IsDisposed) return;

		var active = this._state.IsGPosing && GetTargetAddress() != nint.Zero;
		if (this.IsInGPose != active) {
			this.IsInGPose = active;
			this.OnGPoseUpdate?.InvokeSafely(active);
			Ktisis.Log.Verbose($"GPose state changed: {(active ? "Active" : "Inactive")}");
		}
	}
	
	// GPose Module

	private unsafe delegate GPoseModule* GPoseModuleDelegate(EventFramework* events);

	[Signature("E8 ?? ?? ?? ?? 0F B7 57 3C")]
	private GPoseModuleDelegate? _getGPoseModule = null;

	public unsafe GPoseModule* GetEventModule() {
		var events = EventFramework.Instance();
		if (events == null || this._getGPoseModule is null)
			return null;
		return this._getGPoseModule.Invoke(events);
	}
	
	// GPose Target

	public unsafe GameObject? GetTarget() {
		var tarSys = TargetSystem.Instance();
		var target = tarSys != null ? tarSys->GPoseTarget : null;
		return target is not null ? this._actors.CreateObjectReference((nint)target) : null;
	}

	public nint GetTargetAddress()
		=> GetTarget()?.Address ?? nint.Zero;
	
	// Disposal

	private bool IsDisposed;

	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		this._framework.Update -= OnFrameworkEvent;
	}
}
