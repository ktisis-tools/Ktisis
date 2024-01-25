using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing;

public class PosingManager : IPosingManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	private readonly IFramework _framework;

	public bool IsValid => this._mediator.IsGPosing;

	public PosingManager(
		IContextMediator mediator,
		HookScope scope,
		IFramework framework
	) {
		this._mediator = mediator;
		this._scope = scope;
		this._framework = framework;
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
		
		pose.ApplyToPartial(skeleton, partialId, PoseTransforms.Rotation | PoseTransforms.PositionRoot);
	}
	
	// Pose files

	public Task ApplyPoseFile(
		EntityPose pose,
		PoseFile file,
		PoseTransforms transforms = PoseTransforms.Rotation,
		bool selectedBones = false
	) {
		return this._framework.RunOnFrameworkThread(() => {
			if (file.Bones == null) return;
			
			var converter = new EntityPoseConverter(pose);
			if (selectedBones)
				converter.LoadSelectedBones(file.Bones, transforms);
			else
				converter.Load(file.Bones, transforms);
		});
	}

	public Task<PoseFile> SavePoseFile(EntityPose pose) => this._framework.RunOnFrameworkThread(
		() => new EntityPoseConverter(pose).SaveFile()
	);
	
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
