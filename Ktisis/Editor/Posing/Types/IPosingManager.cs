using System;
using System.Threading.Tasks;

using Ktisis.Data.Files;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing.Types;

public interface IPosingManager : IDisposable {
	public bool IsValid { get; }
	
	public IAttachManager Attachments { get; }

	public PoseMemento? StashedPose { get; set; }
	public DateTime? StashedAt { get; set; }
	public string? StashedFrom { get; set; }
	
	public void Initialize();

	public bool IsEnabled { get; }
	public void SetEnabled(bool enable);
	public Task SyncFaceModelSpace(ActorEntity actor);

	public IIkController CreateIkController();

	public Task ApplyReferencePose(EntityPose pose);

	public Task ApplyPoseFile(EntityPose pose, PoseFile file, PoseMode modes = PoseMode.All, PoseTransforms transforms = PoseTransforms.Rotation, bool selectedBones = false, bool anchorGroups = false, bool excludeEars = false);
	public Task<PoseFile> SavePoseFile(EntityPose pose);

	public Task StashPose(EntityPose pose);
	public Task ApplyStashedPose(EntityPose pose);
}
