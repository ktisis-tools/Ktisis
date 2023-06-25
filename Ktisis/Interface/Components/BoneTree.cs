using System.Linq;
using System.Numerics;

using ImGuiNET;

using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor;
using Ktisis.Util;
using Dalamud.Interface;
using System;

namespace Ktisis.Interface.Components {
	public class BoneTree {
		private enum HighlightReason {
			None = 0,
			Selected = 1,
			Queried = 2,
			ChildSelected = 4,
			ChildQueried = 16
		}

		private static Vector2 _FrameMin;
		private static Vector2 _FrameMax;

		private static string SearchText = "";

		public static unsafe void Draw(Actor* actor) {
			if (ImGui.CollapsingHeader("Bone List")) {
				GuiHelpers.Icon(FontAwesomeIcon.Search);
				ImGui.SameLine();
				ImGui.InputText("##Search", ref SearchText, (uint)(SearchText.Length + 10));

				var lineHeight = ImGui.GetTextLineHeight();
				if (ImGui.BeginChildFrame(471, new Vector2(-1, lineHeight * 12), ImGuiWindowFlags.HorizontalScrollbar)) {
					if (actor == null || actor->Model == null || actor->Model->Skeleton == null)
						return;

					var body = actor->Model->Skeleton->GetBone(0, 1);
					if (body != null && body.Pose != null)
						DrawBoneTreeNode(body);

					ImGui.EndChildFrame();

					_FrameMin = ImGui.GetItemRectMin();
					_FrameMax.X = ImGui.GetItemRectMax().X;
					_FrameMax.Y = _FrameMin.Y + lineHeight * 11;
				}
			}
		}

		private static bool BoneMatchesSearch(Bone bone) => bone.UniqueName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
			|| bone.LocaleName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

		private static void DrawBoneTreeNode(Bone bone) {
			var children = bone.GetChildren();
			var decendents = bone.GetDescendants();

			bool hasChildInQuery = SearchText != "" && decendents.Any(BoneMatchesSearch);
			bool hasChildSelected = decendents.Any(Skeleton.IsBoneSelected);
			bool isSelected = Skeleton.IsBoneSelected(bone);
			bool isQueried = SearchText != "" && BoneMatchesSearch(bone);

			var criteria = HighlightReason.None;

			if (isSelected) criteria |= HighlightReason.Selected;
			if (isQueried) criteria |= HighlightReason.Queried;
			if (hasChildSelected) criteria |= HighlightReason.ChildSelected;
			if (hasChildInQuery) criteria |= HighlightReason.ChildQueried;

			var flag = ImGuiTreeNodeFlags.SpanFullWidth;
			flag |= children.Count > 0 ? ImGuiTreeNodeFlags.OpenOnArrow : ImGuiTreeNodeFlags.Leaf;

			var show = DrawBoneNode(bone, flag, criteria, () => OverlayWindow.SetGizmoOwner(bone.UniqueName));
			if (show) {
				foreach (var child in children)
					DrawBoneTreeNode(child);
				ImGui.TreePop();
			}
		}

		private static bool DrawBoneNode(Bone bone, ImGuiTreeNodeFlags flag, HighlightReason criteria, System.Action? executeIfClicked = null) {
			bool show = ImGui.TreeNodeEx(bone.UniqueId, flag, bone.LocaleName);

			if (criteria != HighlightReason.None)
				ApplyIcons(criteria);

			var rectMin = ImGui.GetItemRectMin() + new Vector2(ImGui.GetTreeNodeToLabelSpacing(), 0);
			var rectMax = ImGui.GetItemRectMax();

			var mousePos = ImGui.GetMousePos();

			var scrollMin = ImGui.GetScrollY();
			var scrollMax = ImGui.GetScrollMaxY();

			if (
				ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& mousePos.X > rectMin.X && mousePos.X < rectMax.X
				&& mousePos.Y > rectMin.Y && mousePos.Y < rectMax.Y
				&& mousePos.X > _FrameMin.X && mousePos.X < _FrameMax.X
				&& mousePos.Y > _FrameMin.Y && mousePos.Y < _FrameMax.Y
			) {
				executeIfClicked?.Invoke();
			}
			return show;
		}

		private static readonly (HighlightReason, FontAwesomeIcon)[] CriteriaIconMap = new [] {
			(HighlightReason.ChildSelected, FontAwesomeIcon.HatWizard),
			(HighlightReason.ChildQueried, FontAwesomeIcon.Search),
			(HighlightReason.Selected, FontAwesomeIcon.WandMagicSparkles),
			(HighlightReason.Queried, FontAwesomeIcon.SearchLocation)
		};

		private static void ApplyIcons(HighlightReason criteria) {
			foreach (var (flag, icon) in CriteriaIconMap) {
				if (criteria.HasFlag(flag)) {
					ImGui.SameLine();
					GuiHelpers.Icon(icon);
				}
			}
		}
	}
}