using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;

using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Transforms;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Decor;
using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Newtonsoft.Json;

namespace Ktisis.Interop.Ipc;

[Singleton]
public class IpcProvider(ContextManager ctxManager, IDalamudPluginInterface dpi)
	{
	private ICallGateProvider<(int, int)> IpcVersion { get; } = dpi.GetIpcProvider<(int, int)>("Ktisis.ApiVersion");
	private ICallGateProvider<bool> IpcRefreshActions { get; } = dpi.GetIpcProvider<bool>("Ktisis.RefreshActors");
	private ICallGateProvider<bool> IpcIsPosing { get; } = dpi.GetIpcProvider<bool>("Ktisis.IsPosing");
	private ICallGateProvider<uint, string, Task<bool>> IpcLoadPose { get; } = dpi.GetIpcProvider<uint, string, Task<bool>>("Ktisis.LoadPose");
	private ICallGateProvider<uint, string, bool, bool, bool, Task<bool>> IpcLoadPoseExtended { get; } = dpi.GetIpcProvider<uint, string, bool, bool, bool, Task<bool>>("Ktisis.LoadPoseExtended");
	private ICallGateProvider<uint, Task<string?>> IpcSavePose { get; } = dpi.GetIpcProvider<uint, Task<string?>>("Ktisis.SavePose");

	private ICallGateProvider<uint, string, Matrix4x4, bool, Task<bool>> IpcSetMatrix { get; } = dpi.GetIpcProvider<uint, string, Matrix4x4, bool, Task<bool>>("Ktisis.SetMatrix");
	private ICallGateProvider<uint, string, bool, Task<Matrix4x4?>> IpcGetMatrix { get; } = dpi.GetIpcProvider<uint, string, bool, Task<Matrix4x4?>>("Ktisis.GetMatrix");
	private ICallGateProvider<uint, List<string>, bool, Task<Dictionary<string, Matrix4x4?>>> IpcBatchGetMatrix { get; } = dpi.GetIpcProvider<uint, List<string>, bool, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.BatchGetMatrix");
	private ICallGateProvider<uint, Dictionary<string, Matrix4x4>, bool, Task<bool>> IpcBatchSetMatrix { get; } = dpi.GetIpcProvider<uint, Dictionary<string, Matrix4x4>, bool, Task<bool>>("Ktisis.BatchSetMatrix");
	private ICallGateProvider<uint, bool, Task<Dictionary<string, Matrix4x4?>>> IpcGetAllMatrices { get; } = dpi.GetIpcProvider<uint, bool, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.GetAllMatrices");
	private ICallGateProvider<Task<Dictionary<int, HashSet<string>>>> IpcSelectedBones { get; } = dpi.GetIpcProvider<Task<Dictionary<int, HashSet<string>>>>("Ktisis.SelectedBones");
	
	#region core
	private (int, int) GetVersion() => (1, 0);

	private bool RefreshActors()
		{
		ctxManager.Current?.Scene.GetModule<ActorModule>().RefreshGPoseActors();
		return true;
	}

	private bool IsActive() => ctxManager.Current?.Posing.IsEnabled ?? false;

	private async Task<bool> LoadPose(uint index, string json, bool rotation, bool position, bool scale)
		{
		var transforms = PoseTransforms.None;
		if (rotation) transforms |= PoseTransforms.Rotation;
		if (position) transforms |= PoseTransforms.Position;
		if (scale) transforms |= PoseTransforms.Scale;

		return await LoadPose(index, json, transforms);
	}

	private async Task<bool> LoadPose(uint index, string json)
		=> await LoadPose(index, json, PoseTransforms.Rotation);

    private async Task<bool> LoadPose(uint index, string json, PoseTransforms transforms )
		{
		if (ctxManager.Current is null)
			return false;

		var file = JsonConvert.DeserializeObject<PoseFile>(json);
		var actor = ctxManager.Current.Scene.GetEntityForIndex(index);

		if (actor is null || file is null)
			return false;

		await ctxManager.Current.Posing.ApplyPoseFile(
			actor.Pose!,
			file,
			transforms: transforms
		);

		return true;
	}

	private async Task<string?> SavePose(uint index)
		{
		if (ctxManager.Current is null)
			return null;

		var actor = ctxManager.Current.Scene.GetEntityForIndex(index);
		if (actor?.Pose is null)
			return null;

		var file = await ctxManager.Current.Posing.SavePoseFile(actor.Pose);
		return JsonConvert.SerializeObject(file);
	}

	private async Task<Dictionary<int, HashSet<string>>> SelectedBones()
		{
		var sceneChildren = ctxManager.Current?.Scene?.Children
			.OfType<ActorEntity>()
			.ToList();

		if (sceneChildren is null || sceneChildren.Count == 0)
			return new ();

		var ret = new Dictionary<int, HashSet<string>>();

		foreach (var actor in sceneChildren)
		{
			if (!actor.IsValid || actor.Pose is null)
				continue;

			ret[actor.Actor.ObjectIndex] =
				actor.Children.OfType<EntityPose>()
					.SelectMany(x => x.Recurse())
					.Where(s => s.IsSelected)
					.OfType<BoneNode>()
					.Select(s => s.Info.Name)
					.ToHashSet();
		}

		return ret;
	}
	#endregion

	#region Matrix IPC 

	private ActorEntity? GetEntity(uint index)
		=> ctxManager.Current?.Scene?.GetEntityForIndex(index);
	private BoneNode? GetParentBone(BoneNode bone)
		=> bone.Pose.Recurse().OfType<BoneNode>().FirstOrDefault(p => bone.IsBoneChildOf(p));

	private async Task<Matrix4x4?> GetMatrix(uint index, string boneName, bool useWorldSpace) {
		var actor = GetEntity(index);
		var bone = actor?.Pose?.FindBoneByName(boneName);
		if (bone is null) return null;
		//ws
		if (useWorldSpace) return bone.GetMatrix();

		// ps relative
		var model = bone.GetMatrixModel();
		if (model == null) return null;

		var parentModel = GetParentBone(bone)?.GetMatrixModel() ?? Matrix4x4.Identity;

		return Matrix4x4.Invert(parentModel, out var inv)
			? model.Value * inv
			: null;
	}

	private async Task<bool> SetMatrix(uint index, string boneName, Matrix4x4 matrix, bool useWorldSpace) {
		var ctx = ctxManager.Current;
		var actor = GetEntity(index);
		var bone = actor?.Pose?.FindBoneByName(boneName);

		if (ctx is null || bone is null) return false;

		var targetMatrix = CalculateWorldMatrix(bone, matrix, useWorldSpace);
		return ApplyBoneTransform(ctx, bone, targetMatrix);
	}

	private async Task<Dictionary<string, Matrix4x4?>> BatchGetMatrix(uint index, List<string> names, bool useWorldSpace) {
		var actor = GetEntity(index);
		var ret = new Dictionary<string, Matrix4x4?>();

		if (actor?.Pose == null) return ret;

		// lookup dict
		var allBones = actor.Pose.Recurse()
			.OfType<BoneNode>()
			.ToDictionary(b => b.Info.Name, b => b);

		foreach (var name in names)
		{
			if (!allBones.TryGetValue(name, out var bone))
			{
				ret[name] = null;
				continue;
			}
			if (useWorldSpace)
			{
				ret[name] = bone.GetMatrix();
			} else
			{
				var model = bone.GetMatrixModel();
				if (model == null)
				{
					ret[name] = null;
					continue;
				}
				var parentModel = GetParentBone(bone)?.GetMatrixModel() ?? Matrix4x4.Identity;
				ret[name] = Matrix4x4.Invert(parentModel, out var inv) ? model.Value * inv : null;
			}
		}

		return ret;
	}

	private async Task<bool> BatchSetMatrix(uint index, Dictionary<string, Matrix4x4> matrices, bool useWorldSpace) {
		var ctx = ctxManager.Current;
		var actor = GetEntity(index);

		if (ctx == null || actor?.Pose == null || matrices.Count == 0) return false;

		var bones = actor.Pose.Recurse().OfType<BoneNode>().ToList();

		// sort because face bones are annoying
		bones.Sort((a, b) => {
			int p = a.Info.PartialIndex.CompareTo(b.Info.PartialIndex);
			return p != 0 ? p : a.Info.BoneIndex.CompareTo(b.Info.BoneIndex);
		});

		bool anySuccess = false;

		foreach (var bone in bones)
		{
			if (!matrices.TryGetValue(bone.Info.Name, out var matrix))
				continue;

			var targetMatrix = CalculateWorldMatrix(bone, matrix, useWorldSpace);

			if (ApplyBoneTransform(ctx, bone, targetMatrix))
				anySuccess = true;
		}

		return anySuccess;
	}

	private async Task<Dictionary<string, Matrix4x4?>> GetAllMatrices(uint index, bool useWorldSpace) {
		var actor = GetEntity(index);
		var ret = new Dictionary<string, Matrix4x4?>();

		if (actor?.Pose == null) return ret;

		foreach (var bone in actor.Pose.Recurse().OfType<BoneNode>())
		{
			if (useWorldSpace)
			{
				ret[bone.Info.Name] = bone.GetMatrix();
			} else
			{
				var model = bone.GetMatrixModel();
				if (model == null)
				{
					ret[bone.Info.Name] = null;
					continue;
				}
				var parentModel = GetParentBone(bone)?.GetMatrixModel() ?? Matrix4x4.Identity;
				ret[bone.Info.Name] = Matrix4x4.Invert(parentModel, out var inv) ? model.Value * inv : null;
			}
		}

		return ret;
	}

	private unsafe Matrix4x4 CalculateWorldMatrix(BoneNode bone, Matrix4x4 inputMatrix, bool inputIsWorldSpace) {
		if (inputIsWorldSpace) return inputMatrix;

		// ActorSpace = ParentSpace * ParentModel
		var parent = GetParentBone(bone);
		var parentModel = parent?.GetMatrixModel() ?? Matrix4x4.Identity;
		var actorSpaceMatrix = inputMatrix * parentModel;

		// Convert Actor Space to World Space
		var skeleton = bone.GetSkeleton();
		if (skeleton == null) return Matrix4x4.Identity;

		var actorTx = new Transform(skeleton->Transform);
		var m = actorSpaceMatrix;

		m.Translation *= actorTx.Scale;

		var root = Matrix4x4.CreateFromQuaternion(actorTx.Rotation)
				 * Matrix4x4.CreateTranslation(actorTx.Position);

		// Result = Matrix * Root
		return m * root;
	}

	private bool ApplyBoneTransform(IEditorContext ctx, BoneNode bone, Matrix4x4 worldTarget) {
		var target = new TransformTarget(bone, new[] { bone });

		var action = ctx.Transform.Begin(target, setup => {
			setup.MirrorRotation = MirrorMode.Inverse;
			setup.ParentBones = true;
			setup.RelativeBones = true;
		});

		action.SetMatrix(worldTarget);
		action.Dispatch();
		return true;
	}

	#endregion
	
	public void RegisterIpc()
		{
		IpcVersion.RegisterFunc(GetVersion);
		IpcRefreshActions.RegisterFunc(RefreshActors);
		IpcIsPosing.RegisterFunc(IsActive);
		IpcLoadPose.RegisterFunc(LoadPose);
		IpcLoadPoseExtended.RegisterFunc(LoadPose);
		IpcSavePose.RegisterFunc(SavePose);
		IpcGetMatrix.RegisterFunc(GetMatrix);
		IpcSetMatrix.RegisterFunc(SetMatrix);
		IpcSelectedBones.RegisterFunc(SelectedBones);
		IpcBatchGetMatrix.RegisterFunc(BatchGetMatrix);
		IpcBatchSetMatrix.RegisterFunc(BatchSetMatrix);
		IpcGetAllMatrices.RegisterFunc(GetAllMatrices);
	}
}