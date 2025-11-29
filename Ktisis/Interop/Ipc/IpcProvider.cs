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
			var boneWorld = bone.GetMatrix();
			if (boneWorld == null) return null;

			var current = bone.Parent;
			Transform? parentTransform = null;
			while (current != null) 
			{
				if (current is ITransform tNode)
				{
					parentTransform = tNode.GetTransform();
					if (parentTransform != null) break;
				}
				current = current.Parent;
			}
			if (parentTransform == null) return null;

			var parentWithoutScale = new Transform(parentTransform.Position, parentTransform.Rotation, Vector3.One);
			if (Matrix4x4.Invert(parentWithoutScale.ComposeMatrix(), out var parentInverse)) 
			{
				return boneWorld.Value * parentInverse;
			}
			return null;
		}
		return null;
	}

	private unsafe bool SetBoneMatrix(IEditorContext ctx, BoneNode bone, Matrix4x4 matrix, BoneSpace space) {
		Matrix4x4 targetWorld = matrix;

		if (space == BoneSpace.Actor)
		{
			var skeleton = bone.GetSkeleton();
			if (skeleton == null) return false;
			var model = new Transform(skeleton->Transform);
			// Convert Model to World
			var m = matrix;
			m.Translation *= model.Scale;
			m = Matrix4x4.Transform(m, model.Rotation);
			m.Translation += model.Position;
			targetWorld = m;
		} else if (space == BoneSpace.Parent)
		{
			var current = bone.Parent;
			Transform? parentTransform = null;
			while (current != null)
			{
				if (current is ITransform tNode)
				{
					parentTransform = tNode.GetTransform();
					if (parentTransform != null) break;
				}
				current = current.Parent;
			}
			if (parentTransform == null) return false;

			var parentWithoutScale = new Transform(parentTransform.Position, parentTransform.Rotation, Vector3.One);
			targetWorld = matrix * parentWithoutScale.ComposeMatrix();
		}

		// Pass bone as primary target
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

	private async Task<Matrix4x4?> GetMatrix(uint index, string boneName, byte spaceCode) {
		var space = (BoneSpace)spaceCode;
		var actor = ctxManager.Current?.Scene?.GetEntityForIndex(index);
		var bone = actor?.Pose?.Recurse().OfType<BoneNode>().FirstOrDefault(b => b.Info.Name == boneName);

		if (bone is null) return null;
		return GetBoneMatrix(bone, space);
	}

	private async Task<bool> SetMatrix(uint index, string boneName, Matrix4x4 matrix, byte spaceCode) {
		var space = (BoneSpace)spaceCode;
		var ctx = ctxManager.Current;
		if (ctx is null) return false;

		var actor = ctx.Scene?.GetEntityForIndex(index);
		var bone = actor?.Pose?.Recurse().OfType<BoneNode>().FirstOrDefault(b => b.Info.Name == boneName);

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
            if (allBones.TryGetValue(name, out var bone))
            {
                ret[name] = GetBoneMatrix(bone, space);
            }
            else
            {
                ret[name] = null;
            }
        }
        return ret;
    }

	private async Task<bool> BatchSetMatrix(uint index, Dictionary<string, Matrix4x4> matrices, byte spaceCode) {
		var space = (BoneSpace)spaceCode;
		var ctx = ctxManager.Current;
		if (ctx is null) return false;

		var actor = ctx.Scene?.GetEntityForIndex(index);
		if (actor == null) return false;

		var allBones = actor.Pose?.Recurse().OfType<BoneNode>().ToDictionary(b => b.Info.Name, b => b);
		if (allBones == null) return false;

		bool anySuccess = false;
		foreach (var kvp in matrices)
		{
			if (allBones.TryGetValue(kvp.Key, out var bone))
			{
				if (SetBoneMatrix(ctx, bone, kvp.Value, space))
					anySuccess = true;
			}
		}
		return anySuccess;
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
    }
}