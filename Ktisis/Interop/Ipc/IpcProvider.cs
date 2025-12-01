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
using Newtonsoft.Json;

namespace Ktisis.Interop.Ipc;

public enum BoneSpace : byte {
	World = 0,
	Actor = 1,
	Parent = 2
}

[Singleton]
public class IpcProvider(ContextManager ctxManager, IDalamudPluginInterface dpi)
	{
	private ICallGateProvider<(int, int)> IpcVersion { get; } = dpi.GetIpcProvider<(int, int)>("Ktisis.ApiVersion");
	private ICallGateProvider<bool> IpcRefreshActions { get; } = dpi.GetIpcProvider<bool>("Ktisis.RefreshActors");
	private ICallGateProvider<bool> IpcIsPosing { get; } = dpi.GetIpcProvider<bool>("Ktisis.IsPosing");
	private ICallGateProvider<uint, string, Task<bool>> IpcLoadPose { get; } = dpi.GetIpcProvider<uint, string, Task<bool>>("Ktisis.LoadPose");
	private ICallGateProvider<uint, string, bool, bool, bool, Task<bool>> IpcLoadPoseExtended { get; } = dpi.GetIpcProvider<uint, string, bool, bool, bool, Task<bool>>("Ktisis.LoadPoseExtended");
	private ICallGateProvider<uint, Task<string?>> IpcSavePose { get; } = dpi.GetIpcProvider<uint, Task<string?>>("Ktisis.SavePose");

	private ICallGateProvider<uint, string, Matrix4x4, byte, Task<bool>> IpcSetMatrix { get; } = dpi.GetIpcProvider<uint, string, Matrix4x4, byte, Task<bool>>("Ktisis.SetMatrix");
	private ICallGateProvider<uint, string, byte, Task<Matrix4x4?>> IpcGetMatrix { get; } = dpi.GetIpcProvider<uint, string, byte, Task<Matrix4x4?>>("Ktisis.GetMatrix");
	private ICallGateProvider<uint, List<string>, byte, Task<Dictionary<string, Matrix4x4?>>> IpcBatchGetMatrix { get; } = dpi.GetIpcProvider<uint, List<string>, byte, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.BatchGetMatrix");
	private ICallGateProvider<uint, Dictionary<string, Matrix4x4>, byte, Task<bool>> IpcBatchSetMatrix { get; } = dpi.GetIpcProvider<uint, Dictionary<string, Matrix4x4>, byte, Task<bool>>("Ktisis.BatchSetMatrix");
	private ICallGateProvider<uint, byte, Task<Dictionary<string, Matrix4x4?>>> IpcGetAllMatrices { get; } = dpi.GetIpcProvider<uint, byte, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.GetAllMatrices");
	private ICallGateProvider<Task<Dictionary<int, HashSet<string>>>> IpcSelectedBones { get; } = dpi.GetIpcProvider<Task<Dictionary<int, HashSet<string>>>>("Ktisis.SelectedBones");

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

	#region Matrix IPC 
	private unsafe Matrix4x4? GetBoneMatrix(BoneNode bone, BoneSpace space)
	{
		if (space == BoneSpace.World) return bone.GetMatrix();
		if (space == BoneSpace.Actor) return bone.GetMatrixModel();
		if (space == BoneSpace.Parent)
		{
			var boneModel = bone.GetMatrixModel();
			if (boneModel == null) return null;
			var parentBone = bone.Pose.Recurse().OfType<BoneNode>()
								 .FirstOrDefault(p => bone.IsBoneChildOf(p));

			Matrix4x4 parentModel = Matrix4x4.Identity;
			if (parentBone != null)
			{
				var pm = parentBone.GetMatrixModel();
				if (pm.HasValue) parentModel = pm.Value;
			}
			if (Matrix4x4.Invert(parentModel, out var parentInverse))
			{
				return boneModel.Value * parentInverse;
			}
			return null;
		}
		return null;
	}

	private unsafe bool SetBoneMatrix(IEditorContext ctx, BoneNode bone, Matrix4x4 matrix, BoneSpace space) 
	{
		Matrix4x4 targetWorld = matrix;
		if (space == BoneSpace.Actor)
		{
			var skeleton = bone.GetSkeleton();
			if (skeleton == null) return false;
			var actorTransform = new Transform(skeleton->Transform);
			var m = matrix;
			m.Translation *= actorTransform.Scale;
			var rootRotPos = Matrix4x4.CreateFromQuaternion(actorTransform.Rotation)
						   * Matrix4x4.CreateTranslation(actorTransform.Position);

			targetWorld = m * rootRotPos;
		} 
		else if (space == BoneSpace.Parent)
		{
			var parentBone = bone.Pose.Recurse().OfType<BoneNode>()
								 .FirstOrDefault(p => bone.IsBoneChildOf(p));

			Matrix4x4 parentModel = Matrix4x4.Identity;
			if (parentBone != null)
			{
				var pm = parentBone.GetMatrixModel();
				if (pm.HasValue) parentModel = pm.Value;
			}

			var targetModel = matrix * parentModel;
			return SetBoneMatrix(ctx, bone, targetModel, BoneSpace.Actor);
		}
		var target = new TransformTarget(bone, [bone]);
		var transformAction = ctx.Transform.Begin(target, setup => {
			setup.MirrorRotation = MirrorMode.Inverse;
			setup.ParentBones = true;
			setup.RelativeBones = true;
		});

		transformAction.SetMatrix(targetWorld);
		transformAction.Dispatch();
		return true;
	}

	private async Task<Matrix4x4?> GetMatrix(uint index, string boneName, byte spaceCode)
	{
		var space = (BoneSpace)spaceCode;
		var actor = ctxManager.Current?.Scene?.GetEntityForIndex(index);
		var bone = actor?.Pose?.FindBoneByName(boneName);

		if (bone is null) return null;
		return GetBoneMatrix(bone, space);
	}

	private async Task<bool> SetMatrix(uint index, string boneName, Matrix4x4 matrix, byte spaceCode)
		{
		var space = (BoneSpace)spaceCode;
		var ctx = ctxManager.Current;
		if (ctx is null) return false;

		var actor = ctx.Scene?.GetEntityForIndex(index);
		var bone = actor?.Pose?.FindBoneByName(boneName);
		if (bone is null) return false;
		return SetBoneMatrix(ctx, bone, matrix, space);
	}

	private async Task<Dictionary<string, Matrix4x4?>> BatchGetMatrix(uint index, List<string> boneNames, byte spaceCode)
	{
		var ret = new Dictionary<string, Matrix4x4?>();
		var space = (BoneSpace)spaceCode;
		var actor = ctxManager.Current?.Scene?.GetEntityForIndex(index);
		if (actor == null) return ret;
		var allBones = actor.Pose?.Recurse().OfType<BoneNode>().ToDictionary(b => b.Info.Name, b => b);
		if (allBones == null) return ret;

		foreach (var name in boneNames)
		{
			if (allBones.TryGetValue(name, out var bone)) ret[name] = GetBoneMatrix(bone, space);
			else ret[name] = null;
		}
		return ret;
	}

	private async Task<bool> BatchSetMatrix(uint index, Dictionary<string, Matrix4x4> matrices, byte spaceCode)
	{
		var space = (BoneSpace)spaceCode;
		var ctx = ctxManager.Current;
		var actor = ctx?.Scene?.GetEntityForIndex(index);

		if (actor?.Pose == null || matrices.Count == 0) return false;

		var allBonesList = actor.Pose.Recurse().OfType<BoneNode>().ToList();
		// sort because facebones are annoying
		allBonesList.Sort((a, b) => {
			int partialCompare = a.Info.PartialIndex.CompareTo(b.Info.PartialIndex);
			if (partialCompare != 0) return partialCompare;

			return a.Info.BoneIndex.CompareTo(b.Info.BoneIndex);
		});

		bool anySuccess = false;
		foreach (var bone in allBonesList)
		{
			if (!matrices.TryGetValue(bone.Info.Name, out var matrix)) continue;

			if (space == BoneSpace.Parent)
			{
				var parentNode = allBonesList.FirstOrDefault(p => bone.IsBoneChildOf(p));
				Matrix4x4 parentModel = Matrix4x4.Identity;
				if (parentNode != null)
				{
					var pm = parentNode.GetMatrixModel();
					if (pm.HasValue)
					{
						parentModel = pm.Value;
					}
				}
				if (SetBoneMatrix(ctx, bone, matrix * parentModel, BoneSpace.Actor)) anySuccess = true;
			}
			else
			{
				if (SetBoneMatrix(ctx, bone, matrix, space)) anySuccess = true;
			}
		}
		return anySuccess;
	}

	private async Task<Dictionary<string, Matrix4x4?>> GetAllMatrices(uint index, byte spaceCode)
	{
		var ret = new Dictionary<string, Matrix4x4?>();
		var space = (BoneSpace)spaceCode;

		var actor = ctxManager.Current?.Scene?.GetEntityForIndex(index);
		if (actor == null) return ret;

		var allBones = actor.Pose?.Recurse().OfType<BoneNode>();
		if (allBones == null) return ret;

		foreach (var bone in allBones)
		{
			ret[bone.Info.Name] = GetBoneMatrix(bone, space);
		}
		return ret;
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