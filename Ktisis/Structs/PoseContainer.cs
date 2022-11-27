using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interop.Hooks;

namespace Ktisis.Structs {
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
				for (var i = 1; i < skeleton->Bones.Length; i++) {
					if (i == partial.ConnectedBoneIndex)
						continue; // Unsupported by .pose files :(

					var bone = modelSkeleton->GetBone(p, i);

					var name = bone.HkaBone.Name.String;
					var transform = bone.AccessModelSpace();

					this[name] = Transform.FromHavok(*transform);
				}
			}
		}

		public unsafe void Apply(Skeleton* modelSkeleton) {
			var partialCt = modelSkeleton->PartialSkeletonCount;
			var partials = modelSkeleton->PartialSkeletons;
			for (var p = 0; p < partialCt; p++) {
				var partial = partials[p];

				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var skeleton = pose->Skeleton;
				for (var i = 1; i < skeleton->Bones.Length; i++) {
					var bone = modelSkeleton->GetBone(p, i);

					var name = bone.HkaBone.Name.String;
					if (TryGetValue(name, out var val)) {
						var transform = bone.AccessModelSpace();
						*transform = val.ToHavok();
					}
				}

				PoseHooks.SyncModelSpaceHook.Original(pose);
			}
		}
	}
}