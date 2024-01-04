using System.Collections.Generic;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

namespace Ktisis.Editor.Posing.Partials;

public class BoneEnumerator {
	protected readonly int Index;
	protected PartialSkeleton Partial;

	public BoneEnumerator(
		int index,
		PartialSkeleton partial
	) {
		this.Index = index;
		this.Partial = partial;
	}

	protected unsafe hkaSkeleton* GetSkeleton() {
		var pose = this.Partial.GetHavokPose(0);
		return pose != null ? pose->Skeleton : null;
	}

	public unsafe IEnumerable<PartialBoneInfo> EnumerateBones() {
		var skeleton = this.GetSkeleton();
		var bones = skeleton->Bones;
		var parents = skeleton->ParentIndices;
		return this.EnumerateBones(bones, parents);
	}

	private IEnumerable<PartialBoneInfo> EnumerateBones(hkArray<hkaBone> bones, hkArray<short> parents) {
		for (var i = 1; i < bones.Length; i++) {
			var hkaBone = bones[i];

			var name = hkaBone.Name.String;
			if (name.IsNullOrEmpty()) continue;

			if (this.Index > 0 && name == "j_ago") continue; // :)

			yield return new PartialBoneInfo {
				Name = name,
				BoneIndex = i,
				ParentIndex = parents[i],
				PartialIndex = this.Index
			};
		}
	}
}
