using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Editor.Context;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Interop.Hooking;

namespace Ktisis.Editor.Posing;

public class PosingManager : IPosingManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;

	public bool IsValid => this._mediator.IsGPosing;

	public PosingManager(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
	}
	
	// Initialization
	
	private PosingModule? Module { get; set; }

	public void Initialize() {
		try {
			this.Module = this._scope.Create<PosingModule>(this);
			this.Module.Initialize();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize posing manager:\n{err}");
		}
	}
	
	// Module wrappers

	public bool IsEnabled => this.Module?.IsEnabled ?? false;

	public void SetEnabled(bool enable) {
		if (enable && !this.IsValid) return;
		this.Module?.SetEnabled(enable);
	}
	
	// Pose preservation

	private readonly Dictionary<ushort, PoseContainer> _savedPoses = new();

	public unsafe void PreservePoseFor(GameObject gameObject, Skeleton* skeleton) {
		var pose = new PoseContainer();
		pose.Store(skeleton);
		this._savedPoses[gameObject.ObjectIndex] = pose;
	}

	public unsafe void RestorePoseFor(GameObject gameObject, Skeleton* skeleton, ushort partialId) {
		if (!this._savedPoses.TryGetValue(gameObject.ObjectIndex, out var pose))
			return;
		
		pose.ApplyToPartial(skeleton, partialId);
	}
	
	// Disposal

	public void Dispose() {
		try {
			this.Module?.Dispose();
			this.Module = null;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose posing manager:\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
