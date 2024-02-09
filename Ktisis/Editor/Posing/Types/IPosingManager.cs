using System;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Data.Files;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing.Types;

public interface IPosingManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public bool IsEnabled { get; }
	public void SetEnabled(bool enable);

	public IIkController CreateIkController();

	public unsafe void PreservePoseFor(GameObject gameObject, Skeleton* skeleton);
	public unsafe void RestorePoseFor(GameObject gameObject, Skeleton* skeleton, ushort partialId);

	public Task ApplyReferencePose(EntityPose pose);

	public Task ApplyPoseFile(EntityPose pose, PoseFile file, PoseTransforms transforms = PoseTransforms.Rotation, bool selectedBones = false);
	public Task<PoseFile> SavePoseFile(EntityPose pose);
}
