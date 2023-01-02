using System;
using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interface;
using Ktisis.Structs.Bones;
using Ktisis.Library.Extensions;
using Ktisis.Scene.Skeletons.Bones;

namespace Ktisis.Scene.Skeletons {
	public class SkeletonObject : Manipulable {
		// Properties

		public Dictionary<int, IntPtr> Resources = new();

		// Manipulable

		public unsafe override string GetName() => "Skeleton";

		public override void Context() { }

		public override void Select() { }

		// Skeleton

		private unsafe Skeleton* GetSkeleton() {
			if (Parent != null && Parent is HasSkeleton parent)
				return parent.GetSkeleton();
			return null;
		}

		// Update bones

		private void UpdateItem(Manipulable item, ref Dictionary<BoneCategory, List<Bone>> categories, ref List<int> cull) {
			BoneCategory? cat = null;

			var append = new List<Manipulable>();
			if (item is BoneGroup) {
				cat = ((BoneGroup)item).Category;
				if (cat != null && categories.TryGetValue(cat, out var bones)) {
					foreach (var bone in bones)
						append.Add(new ObjectBone(bone));
				}
			}

			if (item == this || cat != null) {
				var groups = new Dictionary<BoneCategory, BoneGroup>();

				for (var i = 0; i < item.Children.Count; i++) {
					var child = item.Children[i];
					if (child is BoneGroup group) {
						if (group.Category!.ParentCategory == cat)
							groups.Add(group.Category, group);
					} else if (child is ObjectBone bone) {
						if (cull.Contains(bone.Partial)) {
							item.RemoveChild(bone);
							i--;
						}
					}
				}

				foreach (var (_, category) in BoneCategories.Categories) {
					if (cat != category.ParentCategory)
						continue;

					BoneGroup? group;
					if (!groups.TryGetValue(category, out group))
						group = item.AddChild(new BoneGroup() { Category = category }) as BoneGroup;

					if (group != null) {
						UpdateItem(group, ref categories, ref cull);
						if (group.Children.Count == 0)
							item.RemoveChild(group);
					}
				}
			}

			if (append.Count > 0)
				item.Children.AddRange(append);
		}

		private unsafe void UpdateItems() {
			var skeleton = GetSkeleton();
			if (skeleton == null)
				return;

			var partials = new List<int>();
			var categories = new Dictionary<BoneCategory, List<Bone>>();

			foreach (var partial in skeleton->IterateSkeletons()) {
				var update = false;

				var handle = (IntPtr)partial.SkeletonResourceHandle;

				var x = partial.GetIndex();
				if (Resources.TryGetValue(x, out var res)) {
					update = handle != res;
					Resources[x] = handle;
				} else {
					update = true;
					Resources.Add(x, handle);
				}

				if (update) {
					partials.Add(x);
					partial.ForEachBone((Bone bone) => {
						var cat = bone.GetCategory();
						if (categories.TryGetValue(cat, out var list))
							list.Add(bone);
						else
							categories.Add(cat, new() { bone });
					});
				}
			}

			if (partials.Count > 0) {
				PluginLog.Verbose($"Rebuilding SkeletonObject for {Parent?.GetName()} ({string.Join(", ", partials)})");
				UpdateItem(this, ref categories, ref partials);
			}
		}

		// Overrides

		public unsafe override bool PreDraw() {
			UpdateItems();
			return true;
		}
	}

	public interface HasSkeleton {
		public unsafe abstract Skeleton* GetSkeleton();
	}
}