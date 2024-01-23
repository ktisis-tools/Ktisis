using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Posing.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Scene.Factory.Builders;

public interface IPoseBuilder : IEntityBuilder<EntityPose, IPoseBuilder> {
	public IBoneTreeBuilder BuildBoneTree(int index, uint partialId, PartialSkeleton partial);
}

public interface IBoneTreeBuilder {
	public IBoneTreeBuilder BuildBoneList();
	public IBoneTreeBuilder BuildCategoryMap();
	public void BindTo(EntityPose pose);
}

public sealed class PoseBuilder : EntityBuilderBase<EntityPose, IPoseBuilder>, IPoseBuilder {
	public PoseBuilder(
		ISceneManager scene
	) : base(scene) {
		this.Name = "Pose";
	}

	protected override IPoseBuilder Builder => this;

	protected override EntityPose Build() {
		return new EntityPose(this.Scene, this);
	}

	public IBoneTreeBuilder BuildBoneTree(int index, uint partialId, PartialSkeleton partial) {
		return new BoneTreeBuilder(this.Scene, index, partialId, partial);
	}

	private class BoneTreeBuilder : BoneEnumerator, IBoneTreeBuilder {
		private readonly ISceneManager _scene;
		
		private readonly uint PartialId;

		private readonly Dictionary<BoneCategory, List<PartialBoneInfo>> CategoryMap = new();
		private readonly List<PartialBoneInfo> BoneList = new();
		
		private LocaleManager Locale => this._scene.Context.Locale;
		
		public BoneTreeBuilder(
			ISceneManager scene,
			int index,
			uint partialId,
			PartialSkeleton partial
		) : base(index, partial) {
			this._scene = scene;
			this.PartialId = partialId;
		}
		
		// Bone list

		public unsafe IBoneTreeBuilder BuildBoneList() {
			var skeleton = this.GetSkeleton();
			if (skeleton != null) {
				var bones = this.EnumerateBones();
				this.BoneList.Clear();
				this.BoneList.AddRange(bones);
			}
			return this;
		}
		
		// Category map

		public unsafe IBoneTreeBuilder BuildCategoryMap() {
			this.CategoryMap.Clear();

			var categories = this._scene.Context.Config.Categories;
            
			var skeleton = this.GetSkeleton();
			if (skeleton == null) return this;
			
			// Build map of categories to bones

			foreach (var bone in this.EnumerateBones()) {
				var category = categories.ResolveBestCategory(skeleton, bone.BoneIndex);
				if (category == null) {
					Ktisis.Log.Warning($"Failed to find category for {bone.Name}! Skipping...");
					continue;
				}

				// TODO: Configure this
				if (category.IsNsfw) continue;

				if (this.CategoryMap.TryGetValue(category, out var boneList))
					boneList.Add(bone);
				else
					this.CategoryMap.Add(category, [bone]);
			}
			
			// Ensure parents of categories always exist, even if they don't have bones
			this.BuildOrphanedCategories(categories);
			
			return this;
		}

		private void BuildOrphanedCategories(CategoryConfig categories) {
			var keys = this.CategoryMap.Keys.ToList();
			var orphans = keys.Where(
				category => category.ParentCategory != null && keys.All(x => x.Name != category.ParentCategory)
			).ToList();

			foreach (var category in orphans) {
				var parent = categories.CategoryList
					.Find(x => x.Name == category.ParentCategory);
				
				if (parent == null || this.CategoryMap.ContainsKey(parent))
					continue;

				this.CategoryMap.Add(parent, []);
			}
		}
		
		// Binding

		public void BindTo(EntityPose pose) {
			if (this.CategoryMap.Count > 0)
				this.BindGroups(pose, null);
			if (this.BoneList.Count > 0)
				this.BindBones(pose, this.BoneList);
		}

		private void BindGroups(SkeletonNode node, BoneCategory? parent) {
			var categories = this.CategoryMap
				.Where(x => x.Key.ParentCategory == parent?.Name)
				.ToArray();

			List<BoneNodeGroup>? exists = null;
			var children = node.Children.ToList();
			if (children.Count > 0) {
				exists = children.Where(x => x is BoneNodeGroup)
					.Cast<BoneNodeGroup>()
					.ToList();
			}

			if (exists != null) {
				foreach (var group in exists.Where(
					group => categories.All(cat => cat.Key.Name != group.Name)
				)) {
					this.BindGroups(group, group.Category);
				}
			}

			foreach (var (category, list) in categories) {
				var group = exists?.Find(group => group.Category == category);
				var isNew = group == null;
				group ??= new BoneNodeGroup(this._scene, node.Pose) {
					Name = this.Locale.GetCategoryName(category),
					Category = category,
					SortPriority = category.SortPriority ?? -1
				};
				this.BindGroups(group, category);
				this.BindBones(group, list);
				if (isNew && group.Children.Any())
					node.Add(group);
			}
			
			node.OrderByPriority();
		}

		private void BindBones(SkeletonNode node, List<PartialBoneInfo> bones) {
			List<BoneNode>? exists = null;
			var grpNodes = node.Children.ToList();
			if (grpNodes.Count > 0) {
				exists = grpNodes.Where(x => x is BoneNode bone && bone.Info.PartialIndex == this.Index)
					.Cast<BoneNode>()
					.ToList();
			}

			var basePrio = this.Partial.ConnectedBoneIndex + 1;
			foreach (var boneInfo in bones) {
				var bone = exists?.Find(bone => bone.Info.Name == boneInfo.Name);
				if (bone != null) {
					if (this.Index <= bone.Info.PartialIndex)
						node.Remove(bone);
					else continue;
				}
				
				node.Add(new BoneNode(
					this._scene,
					node.Pose,
					boneInfo,
					this.PartialId
				) {
					Name = this.Locale.GetBoneName(boneInfo),
					SortPriority = basePrio + boneInfo.BoneIndex
				});
			}
			
			node.OrderByPriority();
		}
	}
}