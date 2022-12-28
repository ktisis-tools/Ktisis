using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.Services;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Poses;
using Ktisis.Interface.Dialog;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Workspace {
	public class ActorObject : Manipulable, Transformable {
		// Manipulable

		public int Index;

		public ActorObject(int x) {
			Index = x;
		}

		public override void Select() {
			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
				PluginLog.Information($"Target {Index}");
			} else {
				var ctrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
				EditorService.Select(this, ctrl);
			}
		}
		public override void Context() {
			var ctx = new ContextMenu();

			ctx.AddSection(new() {
				{ "Select", Select },
				{ "Set nickname...", null! }
			});

			ctx.AddSection(new() {
				{ "Open appearance editor", null! },
				{ "Open animation control", null! },
				{ "Open gaze control", null! }
			});

			ctx.Show();
		}

		internal unsafe override void DrawTreeNode() {
			var actor = GetActor();
			if (actor == null) return;

			ImGui.PushStyleColor(ImGuiCol.Text, RootObjectCol);
			var expand = Tree.CollapsibleNode(
				actor->GetNameOrId(),
				EditorService.IsSelected(this) ? ImGuiTreeNodeFlags.Selected : 0,
				Select, Context
			);
			ImGui.PopStyleColor();

			if (expand) {
				var start = Tree.LineStart();

				DrawBoneTree();
				ImGui.TreePop();

				Tree.LineEnd(start, RootObjectCol);
			}
		}

		// Actor

		internal unsafe Actor* GetActor()
			=> (Actor*)DalamudServices.ObjectTable.GetObjectAddress(Index);

		// Skeleton

		private unsafe void DrawBoneTree() {
			var actor = GetActor();
			if (actor == null) return;

			var model = actor->Model;
			if (model == null) return;

			var modelSkele = model->Skeleton;
			if (modelSkele == null) return;

			var bones = new Dictionary<BoneCategory, List<Bone>>();

			var partialCt = modelSkele->PartialSkeletonCount;
			var partials = modelSkele->PartialSkeletons;
			for (var p = 0; p < partialCt; p++) {
				var partial = partials[p];

				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var skeleton = pose->Skeleton;
				for (var i = 0; i < skeleton->Bones.Length; i++) {
					if ((p == 0 && i == 0) || i == partial.ConnectedBoneIndex)
						continue;

					var bone = modelSkele->GetBone(p, i);
					if (p > 0 && bone.HkaBone.Name.String == "j_ago")
						continue;

					var cat = bone.GetCategory();

					if (bones.ContainsKey(cat))
						bones[cat].Add(bone);
					else
						bones.Add(cat, new List<Bone> { bone });
				}
			}

			foreach (var (name, category) in BoneCategories.Categories) {
				if (category.ParentCategory != null)
					continue;

				var exists = bones.ContainsKey(category);
				foreach (var child in category.SubCategories) {
					if (exists) break;
					exists |= bones.ContainsKey(child);
				}
				if (!exists) continue;

				DrawCategoryNode(bones, category);
			}
		}

		private void DrawCategoryNode(Dictionary<BoneCategory, List<Bone>> bones, BoneCategory category) {
			ImGui.PushStyleColor(ImGuiCol.Text, SubCategoryCol);
			var expand = Tree.CollapsibleNode(category.Name, 0);
			ImGui.PopStyleColor();

			if (expand) {
				var start = Tree.LineStart();

				foreach (var child in category.SubCategories)
					DrawCategoryNode(bones, child);

				if (bones.TryGetValue(category, out var boneList)) {
					foreach (var bone in boneList) {
						Tree.LeafNode(bone.LocaleName);
					}
				}

				Tree.LineEnd(start, SubCategoryCol);

				ImGui.TreePop();
			}
		}

		// Transform

		public unsafe object? GetTransform() {
			var actor = GetActor();
			if (actor == null || actor->Model == null) return null;
			return Transform.FromHavok(actor->Model->Transform);
		}

		public unsafe void SetTransform(object trans) {
			var actor = GetActor();
			if (actor == null || actor->Model == null) return;
			actor->Model->Transform = ((Transform)trans).ToHavok();
		}
	}
}