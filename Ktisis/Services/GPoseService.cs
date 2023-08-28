using System;

using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

using Ktisis.Core;
using Ktisis.Common.Extensions;
using Ktisis.Interop.Structs.Event;

namespace Ktisis.Services;

public delegate void GPoseUpdate(bool active);

public class GPoseService : INotifyReady, IDisposable {
	// Service

	private readonly Framework _framework;
	private readonly UiBuilder _uiBuilder;

	public GPoseService(Framework _framework, UiBuilder _uiBuilder) {
		this._framework = _framework;
		this._uiBuilder = _uiBuilder;

		SignatureHelper.Initialise(this);
	}

	public void OnReady() {
		this._framework.Update += OnFrameworkEvent;
	}

	// State

	public bool IsInGPose { get; private set; }

	// Events

	public event GPoseUpdate? OnGPoseUpdate;

	private void OnFrameworkEvent(object _sender) {
		if (this.IsDisposed) return;

		var active = this._uiBuilder.GposeActive && GetTargetAddress() != nint.Zero;
		if (this.IsInGPose != active) {
			this.IsInGPose = active;
			this.OnGPoseUpdate?.InvokeSafely(active);
			PluginLog.Verbose($"GPose state changed: {(active ? "ACTIVE" : "INACTIVE")}");
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

	public unsafe nint GetTargetAddress() {
		var target = TargetSystem.Instance();
		return target != null ? (nint)target->GPoseTarget : nint.Zero;
	}

	// Disposal

	private bool IsDisposed;

	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		this._framework.Update -= OnFrameworkEvent;
	}
}
