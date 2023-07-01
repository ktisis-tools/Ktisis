using System.Linq;
using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Config.Bones;
using Ktisis.Posing.Bones;
using Ktisis.Scenes.Objects;
using Ktisis.Scenes.Objects.Models;

namespace Ktisis.Scenes.Tree; 

public class BoneTreeBuilder {
	private int Index;
	private uint PartialId;
	private PartialSkeleton Partial;
	
	private readonly Dictionary<BoneCategory, List<BoneData>> Bones;

	public BoneTreeBuilder(int index, uint id, PartialSkeleton partial) {
		Index = index;
		PartialId = id;
		Partial = partial;
		Bones = BuildBoneList();
	}
	
	// Bone list
	
	private unsafe Dictionary<BoneCategory, List<BoneData>> BuildBoneList() {
		var result = new Dictionary<BoneCategory, List<BoneData>>();
		
		var pose = Partial.GetHavokPose(0);
		hkaSkeleton* skeleton;
		if (pose == null || (skeleton = pose->Skeleton) == null)
			return result;

		// Build map of categories to bones

		for (var i = 1; i < skeleton->Bones.Length; i++) {
			var hkaBone = skeleton->Bones[i];

			// this should never happen unless the user has a really fucked custom skeleton
			var name = hkaBone.Name.String;
			if (name == null) continue;

			var info = new BoneData {
				Name = name,
				BoneIndex = i,
				ParentIndex = skeleton->ParentIndices[i],
				PartialIndex = Index
			};
			
			var cat = Ktisis.Config.Categories?.ResolveBestCategory(skeleton, i);
			if (cat == null) {
				// If the default category doesn't exist then something's seriously wrong
				PluginLog.Warning("Failed to retrieve categories, this may be a sign of corrupt configuration!");
				break;
			}
			
			if (result.TryGetValue(cat, out var catBones))
				catBones.Add(info);
			else
				result.Add(cat, new List<BoneData> { info });
		}
		
		// Ensure parents of categories always exist, even if they don't have bones

		var orphans = result.Keys.Where(
			cat => cat.ParentCategory is string parent
			&& !result.Keys.Any(x => x.Name == parent)
		).ToArray();

		foreach (var cat in orphans) {
			var parent = Ktisis.Config.Categories?.CategoryList?
				.Find(x => x!.Name == cat.ParentCategory);
			
			if (parent == null || result.ContainsKey(parent)) continue;
			result.Add(parent, new List<BoneData>());
		}

		return result;
	}

	// Handle partial add
	
	public void Add(SceneObject item)
		=> AddGroups(item, null);

	public void Clean(SceneObject item) {
		if (item is not (Armature or BoneGroup)) return;
		item.Children?.ForEach(Clean);
		item.Children?.RemoveAll(IsStale);
	}
	
	// Whether an item should be deleted

	private bool IsStale(SceneObject item) => item switch {
		BoneGroup group => group.Children?.Count == 0,
		Bone bone when bone.PartialIndex == Index => bone.PartialId != PartialId,
		_ => false
	};
	
	// Bone groups

	private void AddGroups(SceneObject item, BoneCategory? parent) {
		var cats = Bones
			.Where(x => x.Key.ParentCategory == parent?.Name)
			.ToArray();

		BoneGroup[]? exists = null;
		if (item.Children?.Count is > 0)
			exists = item.Children?
				.Where(x => x is BoneGroup)
				.Cast<BoneGroup>()
				.ToArray();

		foreach (var pair in cats) {
			var cat = pair.Key;
			var group = exists?.FirstOrDefault(group => group!.Category == cat, null);
			var isNew = group == null;
			group ??= new BoneGroup(cat) {
				Name = cat.Name ?? "Unknown",
				SortPriority = cat.SortPriority ?? -1
			};
			AddGroups(group, cat);
			AddGroupBones(group, pair.Value);
			if (isNew && group.Children?.Count != 0)
				item.Children?.Add(group);
		}
		
		item.SortChildren();
	}

	// Group bones

	private void AddGroupBones(BoneGroup group, List<BoneData> bones) {
		Bone[]? exists = null;
		if (group.Children?.Count is > 0) {
			exists = group.Children?
				.Where(x => x is Bone bone && bone.PartialIndex == Index)
				.Cast<Bone>()
				.ToArray();
		}

		var basePrio = group.SortPriority + Partial.ConnectedParentBoneIndex;
		foreach (var boneInfo in bones) {
			var bone = exists?.FirstOrDefault(bone => bone.Name == boneInfo.Name);
			if (bone != null) {
				bone.PartialId = PartialId;
			} else {
				group.Children?.Add(new Bone(boneInfo.Name, Index, PartialId) {
					SortPriority = basePrio + boneInfo.BoneIndex
				});
			}
		}
		
		group.SortChildren();
	}
}