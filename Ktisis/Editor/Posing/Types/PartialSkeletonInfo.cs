using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Extensions;

namespace Ktisis.Editor.Posing.Types;

public class PartialSkeletonInfo {
	public uint Id;
	public short ConnectedBoneIndex;
	public short ConnectedParentBoneIndex;
	public short[] ParentIds = Array.Empty<short>();

	public PartialSkeletonInfo(uint id) {
		this.Id = id;
	}
	
	public unsafe void CopyPartial(uint id, PartialSkeleton partial) {
		this.Id = id;

		this.ConnectedBoneIndex = partial.ConnectedBoneIndex;
		this.ConnectedParentBoneIndex = partial.ConnectedParentBoneIndex;
		
		var pose = partial.GetHavokPose(0);
		if (pose != null && pose->Skeleton != null)
			this.ParentIds = pose->Skeleton->ParentIndices.Copy();
		else
			this.ParentIds = Array.Empty<short>();
	}

	public IEnumerable<short> GetParentsOf(int id) {
		var parent = this.ParentIds[id];
		while (parent != -1) {
			yield return parent;
			parent = this.ParentIds[parent];
		}
	}
	
	public bool IsBoneDescendantOf(int bone, int descOf) {
		var boneParent = this.ParentIds[bone];
		while (boneParent != -1) {
			if (boneParent == descOf)
				return true;
			boneParent = this.ParentIds[boneParent];
		}
		return false;
	}
}
