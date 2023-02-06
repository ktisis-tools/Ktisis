using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Library.Extensions;
using Ktisis.Scene.Skeletons.Bones;
using Ktisis.Scene.Interfaces;
using Ktisis.Structs.Actor;

namespace Ktisis.Scene.Skeletons {
	public abstract class SkeletonObject : Manipulable, IHasSkeleton, IVisibilityToggle {
		// Properties

		public Dictionary<int, IntPtr> Resources = new();

		public Dictionary<(int p, int i), ObjectBone> BoneMap = new();

		public Dictionary<string, bool> VisibilityMap = new();

		// Manipulable

		public override string Name {
			get => "Skeleton";
			set { }
		}

		public override void Context() { }

		public override void Select() { }

		// Skeleton

		public unsafe abstract Skeleton* GetSkeleton();

		public unsafe abstract ActorModel* GetObject();

		// Visibility

		public bool Visible { get; set; } = false;

		// Update bones

		private void UpdateItem(Manipulable item, ref Dictionary<BoneCategory, List<Bone>> categories, ref List<int> cull) {
			BoneCategory? cat = null;

			var append = new List<ObjectBone>();
			if (item is BoneGroup) {
				cat = ((BoneGroup)item).Category;
				if (cat != null && categories.TryGetValue(cat, out var bones)) {
					foreach (var bone in bones) {
						var manip = new ObjectBone(this, bone);
						append.Add(manip);
					}
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
						var exists = append.FirstOrDefault(a => a.Name == bone.Name);

						if (cull.Contains(bone.Partial)) {
							if (exists != null) {
								append.Remove(exists);
							} else {
								BoneMap.Remove((bone.Partial, bone.Index));
								item.RemoveChild(bone);
								i--;
							}
						}
					}
				}

				foreach (var (_, category) in CategoryService.Categories) {
					if (cat != category.ParentCategory)
						continue;

					BoneGroup? group;
					if (!groups.TryGetValue(category, out group))
						group = item.AddChild(new BoneGroup(this) { Category = category }) as BoneGroup;

					if (group != null) {
						UpdateItem(group, ref categories, ref cull);
						if (group.Children.Count == 0)
							item.RemoveChild(group);
					}
				}
			}

			foreach (var toAdd in append) {
				BoneMap[(toAdd.Partial, toAdd.Index)] = toAdd;
				item.AddChild(toAdd);
			}

			SortChildren(item);
		}

		internal unsafe void UpdateItems() {
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
				PluginLog.Verbose($"Rebuilding SkeletonObject for {Name} ({string.Join(", ", partials)})");
				UpdateItem(this, ref categories, ref partials);
				PluginLog.Verbose($"[{Name}] Now tracking {Resources.Count} resource(s), {BoneMap.Count} bone(s)");
			}
		}

		private void SortChildren(Manipulable item) {
			item.Children.Sort((a, b) => {
				if (a is BoneGroup a1 && b is BoneGroup b1)
					return (a1.Category?.Order ?? 0) - (b1.Category?.Order ?? 0);
				else if (a is ObjectBone a2 && b is ObjectBone b2)
					return a2.Partial == b2.Partial ? a2.Index - b2.Index : a2.Partial - b2.Partial;
				else
					return a is BoneGroup ? -1 : 1;
			});
		}

		// Transform

		public abstract Transform? GetTransform();
		public abstract void SetTransform(Transform trans);

		// Overrides

		public unsafe override bool PreDraw() {
			UpdateItems();
			return true;
		}
	}
}