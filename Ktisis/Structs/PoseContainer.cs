using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs {
	[Serializable]
	public class PoseContainer : Dictionary<string, Transform> {
		// TODO: Make a helper function somewhere for skeleton iteration?

		public unsafe void Store(Skeleton* modelSkeleton) {
			Clear();

			var partialCt = modelSkeleton->PartialSkeletonCount;
			var partials = modelSkeleton->PartialSkeletons;
			for (var p = 0; p < partialCt; p++) {
				var partial = partials[p];

				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var skeleton = pose->Skeleton;
				for (var i = 0; i < skeleton->Bones.Length; i++) {
					if (i == partial.ConnectedBoneIndex)
						continue; // Unsupported by .pose files :(

					var bone = modelSkeleton->GetBone(p, i);
					var name = bone.HkaBone.Name.String;

					var model = bone.AccessModelSpace();

					this[name] = Transform.FromHavok(*model);
				}
			}
		}

		public unsafe void Apply(Skeleton* modelSkeleton, PoseLoadMode mode = PoseLoadMode.Rotation) {
			var partialCt = modelSkeleton->PartialSkeletonCount;
			for (var p = 0; p < modelSkeleton->PartialSkeletonCount; p++)
				ApplyToPartial(modelSkeleton, p, mode);
		}

		public unsafe void ApplyToPartial(Skeleton* modelSkeleton, int p, PoseLoadMode mode = PoseLoadMode.Rotation) {
			var partial = modelSkeleton->PartialSkeletons[p];

			var pose = partial.GetHavokPose(0);
			if (pose == null) return;

			var skeleton = pose->Skeleton;
			for (var i = 0; i < skeleton->Bones.Length; i++) {
				var bone = modelSkeleton->GetBone(p, i);
				var name = bone.HkaBone.Name.String;

				if (TryGetValue(name, out var val)) {
					var model = bone.AccessModelSpace(FFXIVClientStructs.Havok.hkaPose.PropagateOrNot.Propagate);

					var initial = *model;
					var initialPos = initial.Translation.ToVector3();
					var initialRot = initial.Rotation.ToQuat();

					if (p == 0 && i <= 1) {
						var pos = val.Position.ToHavok();
						model->Translation.X = pos.X;
						model->Translation.Y = pos.Y; // unsure about this
						model->Translation.Z = pos.Z;
					}

					if (mode.HasFlag(PoseLoadMode.Rotation))
						model->Rotation = val.Rotation.ToHavok();
					if (mode.HasFlag(PoseLoadMode.Position))
						model->Translation = val.Position.ToHavok();
					if (mode.HasFlag(PoseLoadMode.Scale))
						model->Scale = val.Scale.ToHavok();

					Overlay.Skeleton.PropagateChildren(bone, model, initialPos, initialRot);
				}
			}
		}
	}

	[Flags]
	public enum PoseLoadMode {
		None = 0,
		Rotation = 1,
		Position = 2,
		Scale = 4
	}
}