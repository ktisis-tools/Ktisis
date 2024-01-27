using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Actions.Types;
using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing;

public class PosingManager : IPosingManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	private readonly IFramework _framework;

	public bool IsValid => this._context.IsGPosing;

	public PosingManager(
		IEditorContext context,
		HookScope scope,
		IFramework framework
	) {
		this._context = context;
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
			var initial = converter.Save();
			
			if (selectedBones)
				converter.LoadSelectedBones(file.Bones, transforms);
			else
				converter.Load(file.Bones, transforms);

			var final = converter.Save();
			this._context.Actions.History.Add(new PoseMemento(converter) {
				Transforms = transforms,
				Initial = selectedBones ? converter.FilterSelectedBones(initial) : initial,
				Final = selectedBones ? converter.FilterSelectedBones(final) : final
			});
		});
	}

	public Task<PoseFile> SavePoseFile(EntityPose pose) => this._framework.RunOnFrameworkThread(
		() => new EntityPoseConverter(pose).SaveFile()
	);

	private class PoseMemento(EntityPoseConverter converter) : IMemento {
		public required PoseTransforms Transforms { get; init; }
		public required PoseContainer Initial { get; init; }
		public required PoseContainer Final { get; init; }
		
		public void Restore() {
			if (!converter.IsPoseValid) return;
			converter.Load(this.Initial, this.Transforms);
		}
		
		public void Apply() {
			if (!converter.IsPoseValid) return;
			converter.Load(this.Final, this.Transforms);
		}
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
