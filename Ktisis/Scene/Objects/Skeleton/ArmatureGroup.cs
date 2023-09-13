using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene.Impl;
using Ktisis.Common.Utility;

namespace Ktisis.Scene.Objects.Skeleton;

public abstract class ArmatureGroup : ArmatureNode, IDummy, IVisibility {
	// Armature

	protected void Clean(int pIndex, uint pId) => this.Children.RemoveAll(item => {
		switch (item) {
			case BoneGroup group:
				group.Clean(pIndex, pId);
				return group.IsStale();
			case Bone bone:
				return bone.Data.PartialIndex == pIndex && bone.PartialId != pId;
			default:
				return false;
		}
	});
	
	// IDummy

	public Transform Transform { get; set; } = new();

	private Transform MakeTransform() {
		var transforms = GetIndividualBones()
			.Select(bone => bone.GetTransform())
			.Where(trans => trans != null)
			.Cast<Transform>()
			.ToList();

		var result = new Transform();

		var count = transforms.Count;
		if (count == 0) return result;

		Quaternion rot;
		if (this.GetCommonParent()?.GetTransform() is Transform pTrans) {
			rot = pTrans.Rotation;
		} else {
			var weight = 1f / count;
			rot = transforms
				.Select(t => t.Rotation)
				.Aggregate((a, b) => a * Quaternion.Slerp(Quaternion.Identity, b, weight));
		}
		
		result = transforms.Aggregate(result, (a, b) => {
			a.Position += b.Position;
			a.Scale += b.Scale;
			return a;
		});
        
		result.Position /= count;
		result.Rotation = Quaternion.Normalize(rot);
		result.Scale /= count;
		
		return result;
	}

	public void CalcTransform()
		=> this.Transform = MakeTransform();

	public Transform GetTransform() {
		var calc = MakeTransform();
		if (Vector3.Distance(calc.Position, this.Transform.Position) > 0.1f)
			this.Transform = calc;
		return this.Transform;
	}
	
	// Bone filtering

	public List<Bone> GetIndividualBones() {
		var results = new List<Bone>();
		foreach (var item in RecurseChildren()) {
			switch (item) {
				case ArmatureGroup group:
					results.AddRange(group.GetIndividualBones());
					break;
				case Bone bone:
					results.Add(bone);
					break;
				default:
					continue;
			}
		}

		var armature = GetArmature();
		results = results.Distinct().ToList();
		results.RemoveAll(bone => {
			var boneIx = bone.Data.BoneIndex;

			var partialIx = bone.Data.PartialIndex;
			var partial = armature.GetPartialCache(partialIx);
			if (partial is null) return false;

			var parent = partial.GetParentsOf(boneIx)
				.Any(parentId => results.Any(x => x.MatchesId(partialIx, parentId)));

			if (parent)
				return true;

			if (partialIx == 0)
				return false;

			var rootPartial = armature.GetPartialCache(0);
			if (rootPartial is null)
				return false;

			var connIx = partial.ConnectedParentBoneIndex;
			return rootPartial.GetParentsOf(connIx)
				.Prepend(connIx)
				.Any(id => results.Any(x => x.MatchesId(0, id)));
		});

		return results;
	}

	protected Bone? GetCommonParent() {
		var arm = GetArmature();
		var bones = GetIndividualBones();
		if (bones.Count == 0) return null;

		var lowP = bones.Select(v => v.Data.PartialIndex).Min();
		var partial = arm.GetPartialCache(lowP);
		if (partial == null) return null;
		
		var indices = bones.Select(bone => {
			var p = bone.Data.PartialIndex;
			var i = bone.Data.BoneIndex;
			if (p > lowP)
				i = arm.GetPartialCache(p)?.ConnectedParentBoneIndex ?? -1;
			return i;
		}).Where(i => i != -1).ToList();

		foreach (var parent in partial.ParentIds.Reverse()) {
			var all = indices.All(i => i == parent || partial.IsBoneDescendantOf(i, parent));
			if (all && arm.GetBoneFromMap(lowP, parent) is Bone result)
				return result;
		}
		
		return null;
	}

	// IVisible

	public new bool Visible {
		get {
			var visible = this.Count > 0;
			foreach (var child in GetChildren()) {
				if (child is not IVisibility iVis) continue;
				visible &= iVis.Visible;
				if (!visible) break;
			}
			return visible;
		}
		set {
			foreach (var child in GetChildren()) {
				if (child is not IVisibility iVis) continue;
				iVis.SetVisible(value);
			}
		}
	}

	public bool SetVisible(bool visible) => this.Visible = visible;
}
