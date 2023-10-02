using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Poses {
	public class PoseContainer : Dictionary<string, Transform> {
		// TODO: Make a helper function somewhere for skeleton iteration?

		public unsafe void Store(Skeleton* modelSkeleton) {
			if (modelSkeleton == null) return;

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
						continue;

					var bone = modelSkeleton->GetBone(p, i);
					var name = bone.HkaBone.Name.String!;

					var model = bone.AccessModelSpace();
					this[name] = Transform.FromHavok(*model);
				}
			}
		}

		public unsafe void Apply(Skeleton* modelSkeleton, PoseTransforms trans = PoseTransforms.Rotation) {
			if (modelSkeleton == null) return;
			
			var partialCt = modelSkeleton->PartialSkeletonCount;
			for (var p = 0; p < partialCt; p++)
				ApplyToPartial(modelSkeleton, p, trans);
		}

		public unsafe void ApplyToPartial(Skeleton* modelSkeleton, int p, PoseTransforms trans = PoseTransforms.Rotation, bool setRootPos = false, bool parentPartial = true) {
			if (modelSkeleton == null) return;
			
			var isElezen = false;
			var owner = modelSkeleton->Owner;
			if (owner != null && owner->GetModelType() == CharacterBase.ModelType.Human) {
				var human = (Human*)owner;
				isElezen = human->Customize.Race == (byte)Race.Elezen;
			}

			var partial = modelSkeleton->PartialSkeletons[p];

			var pose = partial.GetHavokPose(0);
			if (pose == null) return;

			if (trans != PoseTransforms.None) {
				var skeleton = pose->Skeleton;
				for (var i = 0; i < skeleton->Bones.Length; i++) {
					var bone = modelSkeleton->GetBone(p, i);
					var name = bone.HkaBone.Name.String ?? "";

					if (isElezen && name is "j_mimi_l" or "j_mimi_r")
						continue;

					if (TryGetValue(name, out var val)) {
						var model = bone.AccessModelSpace();

						var initial = *model;
						var initialPos = initial.Translation.ToVector3();
						var initialRot = initial.Rotation.ToQuat();

						if (p == 0 && bone.ParentId < 1 && setRootPos) {
							var pos = val.Position.ToHavok();
							model->Translation = pos;
							initialRot = val.Rotation; // idk why this hack works but it does
						}

						if (trans.HasFlag(PoseTransforms.Rotation))
							model->Rotation = val.Rotation.ToHavok();
						if (trans.HasFlag(PoseTransforms.Position))
							model->Translation = val.Position.ToHavok();
						if (trans.HasFlag(PoseTransforms.Scale))
							model->Scale = val.Scale.ToHavok();

						bone.PropagateChildren(model, initialPos, initialRot, true);
					}
				}
			}

			if (p > 0 && parentPartial)
				modelSkeleton->ParentPartialToRoot(p);
		}
	}

	[Flags]
	public enum PoseTransforms {
		None = 0,
		Rotation = 1,
		Position = 2,
		Scale = 4
	}

	[Flags]
	public enum PoseMode {
		None = 0,
		Body = 1,
		Face = 2,
		BodyFace = 3,
		Weapons = 4,
		All = 7
	}
}
