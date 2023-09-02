using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing.Bones;

namespace Ktisis.Scene.Objects.Tree; 

public class BoneEnumerator {
	// Constructor
	
	protected readonly int Index;
	protected PartialSkeleton Partial;

	public BoneEnumerator(int index, PartialSkeleton partial) {
		this.Index = index;
		this.Partial = partial;
	}
	
	// Skeleton access

	protected unsafe hkaSkeleton* GetSkeleton() {
		var pose = this.Partial.GetHavokPose(0);
		return pose != null ? pose->Skeleton : null;
	}
	
	public unsafe IEnumerable<BoneData> EnumerateBones() {
		var skeleton = this.GetSkeleton();
		var bones = skeleton->Bones;
		var parents = skeleton->ParentIndices;
		return EnumerateBones(bones, parents);
	}

	private IEnumerable<BoneData> EnumerateBones(hkArray<hkaBone> bones, hkArray<short> parents) {
		for (var i = 1; i < bones.Length; i++) {
			var hkaBone = bones[i];
			
			// This should never happen unless the user has a really fucked custom skeleton.
			var name = hkaBone.Name.String;
			if (name == null) continue;

			if (this.Index > 0 && name == "j_ago") // :)
				continue;

			yield return new BoneData {
				Name = name,
				BoneIndex = i,
				ParentIndex = parents[i],
				PartialIndex = this.Index
			};
		}
	}
}