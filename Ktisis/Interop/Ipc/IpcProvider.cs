using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;

using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Data;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules.Actors;

namespace Ktisis.Interop.Ipc;

[Singleton]
public class IpcProvider(ContextManager ctxManager, IDalamudPluginInterface dpi, JsonFileSerializer fileSerializer) : IDisposable {
	private ICallGateProvider<(int, int)> IpcVersion { get; } = dpi.GetIpcProvider<(int, int)>("Ktisis.ApiVersion");
	private ICallGateProvider<bool> IpcRefreshActions { get; } = dpi.GetIpcProvider<bool>("Ktisis.RefreshActors");
	private ICallGateProvider<bool> IpcIsPosing { get; } = dpi.GetIpcProvider<bool>("Ktisis.IsPosing");
	private ICallGateProvider<uint, string, Task<bool>> IpcLoadPose { get; } = dpi.GetIpcProvider<uint, string, Task<bool>>("Ktisis.LoadPose");
	private ICallGateProvider<uint, string, bool, bool, bool, Task<bool>> IpcLoadPoseExtended { get; } = dpi.GetIpcProvider<uint, string, bool, bool, bool, Task<bool>>("Ktisis.LoadPoseExtended");
	private ICallGateProvider<uint, Task<string?>> IpcSavePose { get; } = dpi.GetIpcProvider<uint, Task<string?>>("Ktisis.SavePose");

	private ICallGateProvider<Task<Dictionary<int, HashSet<string>>>> IpcSelectedBones { get; } = dpi.GetIpcProvider<Task<Dictionary<int, HashSet<string>>>>("Ktisis.SelectedBones");
	private ICallGateProvider<bool, bool> IpcPosingChangedEvent { get; } = dpi.GetIpcProvider<bool, bool>("Ktisis.PosingChanged");
	private ICallGateProvider<uint, Dictionary<string, Matrix4x4>, Task<bool>> IpcApplyAbsolutePoses { get; } = dpi.GetIpcProvider<uint, Dictionary<string, Matrix4x4>, Task<bool>>("Ktisis.ApplyAbsolutePoses");

	#region core

	private (int, int) GetVersion() => (1, 0);

	private bool RefreshActors() {
		ctxManager.Current?.Scene.GetModule<ActorModule>().RefreshGPoseActors();
		return true;
	}

	private bool IsActive() => ctxManager.Current?.Posing.IsEnabled ?? false;

	private async Task<bool> LoadPose(uint index, string json, bool rotation, bool position, bool scale) {
		var transforms = PoseTransforms.None;
		if (rotation) transforms |= PoseTransforms.Rotation;
		if (position) transforms |= PoseTransforms.Position;
		if (scale) transforms |= PoseTransforms.Scale;

		return await LoadPose(index, json, transforms);
	}

	private async Task<bool> LoadPose(uint index, string json)
		=> await LoadPose(index, json, PoseTransforms.Rotation);

	private async Task<bool> LoadPose(uint index, string json, PoseTransforms transforms) {
		if (ctxManager.Current is null)
			return false;

		var file = fileSerializer.Deserialize<PoseFile>(json);
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

	private async Task<string?> SavePose(uint index) {
		if (ctxManager.Current is null)
			return null;

		var actor = ctxManager.Current.Scene.GetEntityForIndex(index);
		if (actor?.Pose is null)
			return null;

		var file = await ctxManager.Current.Posing.SavePoseFile(actor.Pose);
		return fileSerializer.Serialize(file);
	}

	private async Task<Dictionary<int, HashSet<string>>> SelectedBones() {
		var sceneChildren = ctxManager.Current?.Scene?.Children
			.OfType<ActorEntity>()
			.ToList();

		if (sceneChildren is null || sceneChildren.Count == 0)
			return new();

		var ret = new Dictionary<int, HashSet<string>>();

		foreach (var actor in sceneChildren) {
			if (!actor.IsValid || actor.Pose is null)
				continue;

			ret[actor.Actor.ObjectIndex] =
				actor.Children.OfType<EntityPose>()
					.SelectMany(x => x.Recurse().Append(x))
					.Where(s => s.IsSelected)
					.SelectMany(s => s.Recurse().Append(s))
					.OfType<BoneNode>()
					.Select(s => s.Info.Name)
					.ToHashSet();
		}

		return ret;
	}

	#endregion

	#region Absolute Posing IPC

	private ActorEntity? GetEntity(uint index) => ctxManager.Current?.Scene?.GetEntityForIndex(index);
	private BoneNode? GetParentBone(BoneNode bone) => bone.Pose.Recurse().OfType<BoneNode>().FirstOrDefault(p => bone.IsBoneChildOf(p));

	/// <summary>
	/// Local Pos/Rot, Model Scale to circumvent Racial Offset
	/// </summary>
	private async Task<bool> ApplyAbsolutePoses(uint index, Dictionary<string, Matrix4x4> matrices) {
		var actor = GetEntity(index);
		if (actor?.Pose == null || matrices.Count == 0) return false;

		unsafe {
			var skeleton = actor.Pose.GetSkeleton();
			if (skeleton == null) return false;
			
			foreach (var kvp in matrices) // local pos/rot
			{
				var bone = actor.Pose.FindBoneByName(kvp.Key);
				if (bone == null) continue;

				var pose = bone.GetPose();
				if (pose == null || pose->LocalPose.Data == null) continue;

				Matrix4x4.Decompose(kvp.Value, out _, out var rot, out var pos);

				var qsLocal = pose->LocalPose.Data + bone.Info.BoneIndex;
				qsLocal->Translation = new hkVector4f { X = pos.X, Y = pos.Y, Z = pos.Z, W = 0f };
				qsLocal->Rotation = new hkQuaternionf { X = rot.X, Y = rot.Y, Z = rot.Z, W = rot.W };
			}
			for (int pIndex = 0; pIndex < skeleton->PartialSkeletonCount; pIndex++) {
				HavokPosing.SyncModelSpace(skeleton, pIndex);
			}
			
			foreach (var kvp in matrices) //overwrite with model scale
			{
				var bone = actor.Pose.FindBoneByName(kvp.Key);
				if (bone == null) continue;
				
				var pose = bone.GetPose();
				if (pose == null || pose->ModelPose.Data == null) continue;
				
				Matrix4x4.Decompose(kvp.Value, out var scale, out _, out _);
				
				var qsModel = pose->ModelPose.Data + bone.Info.BoneIndex;
				qsModel->Scale = new hkVector4f { X = scale.X, Y = scale.Y, Z = scale.Z, W = 0f };
			}
		}
		
		return true;
	}
	#endregion

	public void InvokePosingChanged(bool status) {
		this.IpcPosingChangedEvent.SendMessage(status);
	}

	public void RegisterIpc() {
		IpcVersion.RegisterFunc(GetVersion);
		IpcRefreshActions.RegisterFunc(RefreshActors);
		IpcIsPosing.RegisterFunc(IsActive);
		IpcLoadPose.RegisterFunc(LoadPose);
		IpcLoadPoseExtended.RegisterFunc(LoadPose);
		IpcSavePose.RegisterFunc(SavePose);
		IpcSelectedBones.RegisterFunc(SelectedBones);
		IpcApplyAbsolutePoses.RegisterFunc(ApplyAbsolutePoses);
		// IpcPosingChangedEvent.RegisterFunc(); no func to register since we're firing messages
	}

	private void UnregisterIpc() {
		IpcVersion.UnregisterFunc();
		IpcRefreshActions.UnregisterFunc();
		IpcIsPosing.UnregisterFunc();
		IpcLoadPose.UnregisterFunc();
		IpcLoadPoseExtended.UnregisterFunc();
		IpcSavePose.UnregisterFunc();
		IpcSelectedBones.UnregisterFunc();
		IpcPosingChangedEvent.UnregisterFunc();
		IpcApplyAbsolutePoses.UnregisterFunc();
	}

	public void Dispose() {
		Ktisis.Log.Info("Disposing Ktisis IPC Provider.");
		this.UnregisterIpc();
	}
}
