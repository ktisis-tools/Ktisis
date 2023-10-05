using System.Linq;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing.Bones;
using Ktisis.Data.Config.Bones;
using Ktisis.Scene.Objects.Skeleton;

namespace Ktisis.Scene.Objects.Tree;

public class BoneTreeBuilder : BoneEnumerator {
	// State

	private readonly uint PartialId;

	private readonly List<BoneData>? BoneList;
	private readonly Dictionary<BoneCategory, List<BoneData>>? CategoryMap;

	private readonly SceneContext _ctx;
	
	// Constructor

	public BoneTreeBuilder(int index, uint pId, PartialSkeleton partial, Categories? _buildCats, SceneContext _ctx) : base(index, partial) {
		this.PartialId = pId;

		if (_buildCats is not null)
			this.CategoryMap = BuildCategoryMap(_buildCats);
		else
			this.BoneList = BuildBoneList();

		this._ctx = _ctx;
	}

	// Bone list

	private unsafe List<BoneData> BuildBoneList() {
		var result = new List<BoneData>();

		var skeleton = this.GetSkeleton();
		if (skeleton == null)
			return result;

		var bones = EnumerateBones();
		result.AddRange(bones);

		return result;
	}

	// Category map

	private unsafe Dictionary<BoneCategory, List<BoneData>> BuildCategoryMap(Categories cats) {
		var result = new Dictionary<BoneCategory, List<BoneData>>();

		var skeleton = this.GetSkeleton();
		if (skeleton == null)
			return result;

		// Build map of categories to bones

		foreach (var bone in EnumerateBones()) {
			var cat = cats.ResolveBestCategory(skeleton, bone.BoneIndex);
			if (cat is null) continue;

			// TODO: Config
			if (cat.IsNsfw) continue;

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

		var armature = item.GetArmature();
		foreach (var (cat, list) in cats) {
			var group = exists?.Find(group => group.Category == cat);
			var isNew = group is null;
			group ??= new BoneGroup(armature, cat) {
				Name = this._ctx.GetCategoryName(cat),
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
				.Where(x => x is Bone bone && bone.Data.PartialIndex == this.Index)
				.Cast<Bone>()
				.ToList();
		}

		var armature = group.GetArmature();
		var basePrio = this.Partial.ConnectedBoneIndex + 1;
		foreach (var boneData in bones) {
			var bone = exists?.Find(bone => bone.Data.Name == boneData.Name);
			if (bone is not null) {
				if (this.Index <= bone.Data.PartialIndex)
					group.RemoveChild(bone);
				else continue;
			}

			group.AddChild(new Bone(armature, boneData, this.PartialId) {
				Name = this._ctx.GetBoneName(boneData),
				SortPriority = basePrio + boneData.BoneIndex
			});
		}

		group.OrderByPriority();
	}
}
