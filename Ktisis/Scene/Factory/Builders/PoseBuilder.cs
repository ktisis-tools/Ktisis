using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Data.Config;
using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Posing.Ik.Ccd;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Editor.Posing.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Skeleton.Constraints;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Builders;

public interface IPoseBuilder : IEntityBuilder<EntityPose, IPoseBuilder> {
	public IBoneTreeBuilder BuildBoneTree(int index, uint partialId, PartialSkeleton partial);
}

public interface IBoneTreeBuilder {
	public IBoneTreeBuilder BuildBoneList();
	public IBoneTreeBuilder BuildCategoryMap();
	public void BindTo(EntityPose pose);
}

public sealed class PoseBuilder : EntityBuilder<EntityPose, IPoseBuilder>, IPoseBuilder {
	public PoseBuilder(
		ISceneManager scene
	) : base(scene) {
		this.Name = "Pose";
	}

	protected override IPoseBuilder Builder => this;

	protected override EntityPose Build() {
		var ik = this.Scene.Context.Posing.CreateIkController();
		var pose = new EntityPose(this.Scene, this, ik);
		ik.Setup(pose);
		return pose;
	}

	public IBoneTreeBuilder BuildBoneTree(int index, uint partialId, PartialSkeleton partial) {
		return new BoneTreeBuilder(this.Scene, index, partialId, partial);
	}

	private class BoneTreeBuilder : BoneEnumerator, IBoneTreeBuilder {
		private readonly ISceneManager _scene;
		
		private readonly uint PartialId;

		private readonly Dictionary<BoneCategory, List<PartialBoneInfo>> CategoryMap = new();
		private readonly List<PartialBoneInfo> BoneList = new();

		private Configuration Config => this._scene.Context.Config;
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

			var categories = this.Config.Categories;
            
			var skeleton = this.GetSkeleton();
			if (skeleton == null) return this;
			
			// Build map of categories to bones

			foreach (var bone in this.EnumerateBones()) {
				var category = categories.ResolveBestCategory(skeleton, bone.BoneIndex);
				if (category == null) {
					Ktisis.Log.Warning($"Failed to find category for {bone.Name}! Skipping...");
					continue;
				}
				
				if (category.IsNsfw && !this.Config.Categories.ShowNsfwBones)
					continue;

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

				group ??= this.CreateGroupNode(node.Pose, category);
				group.Name = this.Locale.GetCategoryName(category);
				group.Category = category;
				group.SortPriority = category.SortPriority ?? -1;
				
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
					if (this.Index != bone.Info.PartialIndex) {
						node.Remove(bone);
						continue;
					}
					bone.Info = boneInfo;
					bone.PartialId = this.PartialId;
				} else {
					var boneNode = this.CreateBoneNode(node, boneInfo);
					boneNode.Name = this.Locale.GetBoneName(boneInfo);
					boneNode.SortPriority = basePrio + boneInfo.BoneIndex;
					node.Add(boneNode);
				}
			}
			
			node.OrderByPriority();
		}

		private BoneNodeGroup CreateGroupNode(EntityPose pose, BoneCategory category) {
			var name = category.Name;
			switch (category) {
				case { TwoJointsGroup: {} param } when pose.IkController.TrySetupGroup(name, param, out var group):
					return new IkNodeGroup<TwoJointsGroup>(this._scene, pose, group!);
				case { CcdGroup: {} param } when pose.IkController.TrySetupGroup(name, param, out var group):
					return new IkNodeGroup<CcdGroup>(this._scene, pose, group!);
				default:
					return new BoneNodeGroup(this._scene, pose);
			}
		}

		private BoneNode CreateBoneNode(SkeletonNode parent, PartialBoneInfo boneInfo) {
			switch (parent) {
				case IkNodeGroup<TwoJointsGroup> tjNode when tjNode.Group.EndBoneIndex == boneInfo.BoneIndex:
					return new TwoJointEndNode(this._scene, parent.Pose, boneInfo, this.PartialId, tjNode.Group);
				case IkNodeGroup<CcdGroup> ccdNode when ccdNode.Group.EndBoneIndex == boneInfo.BoneIndex:
					return new CcdEndNode(this._scene, parent.Pose, boneInfo, this.PartialId, ccdNode.Group);
				default:
					return new BoneNode(this._scene, parent.Pose, boneInfo, this.PartialId);
			}
		}
	}
}