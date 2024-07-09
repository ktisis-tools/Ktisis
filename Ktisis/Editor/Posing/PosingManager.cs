using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Editor.Posing.AutoSave;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Editor.Posing.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Posing;

public class PosingManager : IPosingManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	private readonly IFramework _framework;

	public bool IsValid => this._context.IsValid;
	
	public IAttachManager Attachments { get; }

	private readonly PoseAutoSave AutoSave;

	public PosingManager(
		IEditorContext context,
		HookScope scope,
		IFramework framework,
		IAttachManager attach,
		PoseAutoSave autoSave
	) {
		this._context = context;
		this._scope = scope;
		this._framework = framework;
		this.Attachments = attach;
		this.AutoSave = autoSave;
	}
	
	// Initialization
	
	public bool IsSolvingIk { get; set; }
	
	private PosingModule? PoseModule { get; set; }
	private IkModule? IkModule { get; set; }

	public void Initialize() {
		try {
			this.PoseModule = this._scope.Create<PosingModule>(this);
			this.PoseModule.Initialize();
			
			this.IkModule = this._scope.Create<IkModule>(this);
			this.IkModule.Initialize();
			
			this.AutoSave.Initialize(this._context.Config);
			
			this.Subscribe();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize posing manager:\n{err}");
		}
	}
	
	// Events

	private unsafe void Subscribe() {
		this.PoseModule!.OnSkeletonInit += this.OnSkeletonInit;
		this._context.Characters.OnDisableDraw += this.OnDisableDraw;
		this._context.Plugin.Config.OnSaved += this.AutoSave.Configure;
	}

	private unsafe void OnSkeletonInit(IGameObject gameObject, Skeleton* skeleton, ushort partialId) {
		this.RestorePoseFor(gameObject.ObjectIndex, skeleton, partialId);
	}

	private unsafe void OnDisableDraw(IGameObject gameObject, DrawObject* drawObject) {
		Ktisis.Log.Verbose($"Preserving state for {gameObject.Name} ({gameObject.ObjectIndex})");
		
		var skeleton = gameObject.GetSkeleton();
		if (skeleton == null) return;

		this.Attachments.Invalidate(skeleton);
		this.PreservePoseFor(gameObject.ObjectIndex, skeleton);
	}
	
	// Module wrappers

	public bool IsEnabled => this.PoseModule?.IsEnabled ?? false;

	public void SetEnabled(bool enable) {
		if (enable && !this.IsValid) return;
		this.PoseModule?.SetEnabled(enable);
	}

	public IIkController CreateIkController() => this.IkModule!.CreateController();
	
	// Skeleton state

	private readonly Dictionary<ushort, PoseState> _savedPoses = new();

	private unsafe void PreservePoseFor(ushort objectIndex, Skeleton* skeleton) {
		var trans = skeleton->Owner != null ? ((CharacterBaseEx*)skeleton->Owner)->Transform : skeleton->Transform;
		var pose = new PoseContainer();
		pose.Store(skeleton);
		this._savedPoses[objectIndex] = new PoseState {
			Pose = pose,
			Transform = new Transform(trans)
		};
	}

	private unsafe void RestorePoseFor(ushort objectIndex, Skeleton* skeleton, ushort partialId) {
		if (!this._savedPoses.TryGetValue(objectIndex, out var state)) return;
		state.Pose.ApplyToPartial(skeleton, partialId, PoseTransforms.Rotation | PoseTransforms.PositionRoot);
		if (partialId == 0 && skeleton->Owner != null)
			((CharacterBaseEx*)skeleton->Owner)->Transform = state.Transform;
	}

	private record PoseState {
		public required Transform Transform;
		public required PoseContainer Pose;
	}
	
	// Pose loading & saving
	
	public Task ApplyReferencePose(EntityPose pose) {
		return this._framework.RunOnFrameworkThread(() => {
			var converter = new EntityPoseConverter(pose);
			var initial = converter.Save();
			converter.LoadReferencePose();
			var final = converter.Save();
			this._context.Actions.History.Add(new PoseMemento(converter) {
				Transforms = PoseTransforms.Position | PoseTransforms.Rotation,
				Bones = null,
				Initial = initial,
				Final = final
			});
		});
	}

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
			
			this._context.Actions.History.Add(new PoseMemento(converter) {
				Transforms = transforms,
				Bones = selectedBones ? converter.GetSelectedBones().ToList() : null,
				Initial = selectedBones ? converter.FilterSelectedBones(initial) : initial,
				Final = selectedBones ? converter.FilterSelectedBones(file.Bones) : file.Bones
			});
		});
	}

	public Task<PoseFile> SavePoseFile(EntityPose pose) => this._framework.RunOnFrameworkThread(
		() => new EntityPoseConverter(pose).SaveFile()
	);
	
	// Disposal

	public void Dispose() {
		try {
			this.PoseModule?.Dispose();
			this.PoseModule = null;
			
			this.IkModule?.Dispose();
			this.IkModule = null;
			
			this.Attachments.Dispose();
			
			this._context.Plugin.Config.OnSaved -= this.AutoSave.Configure;
			this.AutoSave.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose posing manager:\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
