using System.Linq;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing.Skeleton;
using Ktisis.Data.Config.Bones;
using Ktisis.Scene.Objects.Models;

namespace Ktisis.Scene.Objects.Tree; 

public class BoneTreeBuilder {
	// State
	
	private readonly int Index;
	private readonly uint PartialId;
	private PartialSkeleton Partial;

	private readonly List<BoneData>? BoneList;
	private readonly Dictionary<BoneCategory, List<BoneData>>? CategoryMap;
	
	// Constructor

	public BoneTreeBuilder(int index, uint pId, PartialSkeleton partial, Categories? _buildCats) {
		this.Index = index;
		this.PartialId = pId;
		this.Partial = partial;

		if (_buildCats is not null)
			this.CategoryMap = BuildCategoryMap(_buildCats);
		else
			this.BoneList = BuildBoneList();
	}
	
	// Bone list

	private List<BoneData> BuildBoneList() {
		var result = new List<BoneData>();

		return result;
	}
	
	// Category map

	private unsafe Dictionary<BoneCategory, List<BoneData>> BuildCategoryMap(Categories cats) {
		var result = new Dictionary<BoneCategory, List<BoneData>>();

		var skeleton = this.GetSkeleton();
		if (skeleton == null)
			return result;
		
		// Build map of categories to bones

		foreach (var bone in EnumerateBones(skeleton->Bones, skeleton->ParentIndices)) {
			var cat = cats.ResolveBestCategory(skeleton, bone.BoneIndex);
			if (cat is null) continue;

			if (result.TryGetValue(cat, out var boneList))
				boneList.Add(bone);
			else
				result.Add(cat, new List<BoneData> { bone });
		}
		
		// Ensure parents of categories always exist, even if they don't have bones

		var orphans = result.Keys.Where(
			cat => cat.ParentCategory is string parent
			&& !result.Keys.Any(x => x.Name == parent)
		).ToList();

		foreach (var cat in orphans) {
			var parent = cats.CategoryList
				.Find(x => x.Name == cat.ParentCategory);

			if (parent is null || result.ContainsKey(parent))
				continue;
			
			result.Add(parent, new List<BoneData>());
		}
		
		return result;
	}
	
	// Update armature

	public void AddToArmature(Armature arm) {
		if (this.CategoryMap is not null)
			this.AddGroups(arm, null);
		if (this.BoneList is not null)
			this.AddGroupBones(arm, this.BoneList);
	}

	private void AddGroups(ArmatureNode item, BoneCategory? parent) {
		var cats = this.CategoryMap!
			.Where(x => x.Key.ParentCategory == parent?.Name)
			.ToArray();

		List<BoneGroup>? exists = null;
		var children = item.GetChildren();
		if (children.Count > 0) {
			exists = children
				.Where(x => x is BoneGroup)
				.Cast<BoneGroup>()
				.ToList();
		}

		foreach (var (cat, list) in cats) {
			var group = exists?.Find(group => group.Category == cat);
			var isNew = group is null;
			group ??= new BoneGroup(cat) {
				SortPriority = cat.SortPriority ?? -1
			};
			AddGroups(group, cat);
			AddGroupBones(group, list);
			if (isNew && group.Count != 0)
				item.AddChild(group);
		}
		
		item.OrderByPriority();
	}

	private void AddGroupBones(ArmatureNode group, List<BoneData> bones) {
		List<Bone>? exists = null;
		
		var grpChildren = group.GetChildren();
		if (grpChildren.Count > 0) {
			exists = grpChildren
				.Where(x => x is Bone bone && bone.PartialIndex == this.Index)
				.Cast<Bone>()
				.ToList();
		}

		var basePrio = this.Partial.ConnectedBoneIndex + 1;
		foreach (var boneInfo in bones) {
			var bone = exists?.Find(bone => bone.Name == boneInfo.Name);
			if (bone is not null) {
				bone.PartialId = this.PartialId;
			} else {
				group.AddChild(new Bone(boneInfo.Name, this.PartialId, this.Index) {
					SortPriority = basePrio + boneInfo.BoneIndex
				});
			}
		}
		
		group.OrderByPriority();
	}
	
	// Skeleton access

	private unsafe hkaSkeleton* GetSkeleton() {
		var pose = this.Partial.GetHavokPose(0);
		return pose != null ? pose->Skeleton : null;
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