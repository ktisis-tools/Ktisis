using System.Numerics;

using ImGuiNET;

using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor;
using System.Linq;

namespace Ktisis.Interface.Components {
	public class BoneTree {

		public static unsafe void Draw(Actor* actor) {
			if (ImGui.CollapsingHeader("Bone List")) {
				if (ImGui.BeginChildFrame(471, new Vector2(-1, ImGui.GetTextLineHeight() * 12),ImGuiWindowFlags.HorizontalScrollbar)) {
					var body = actor->Model->Skeleton->GetBone(0, 1);
					DrawBoneTreeNode(body);
					ImGui.EndChildFrame();
				}
			}
		}
		private unsafe static void DrawBoneTreeNode(Bone bone) {
			var children = bone.GetChildren();

			var flag = children.Count > 0 ? ImGuiTreeNodeFlags.OpenOnArrow : ImGuiTreeNodeFlags.Leaf;
			if (Skeleton.IsBoneSelected(bone))
				flag |= ImGuiTreeNodeFlags.Selected;

			var show = DrawBoneNode(bone, flag, () => OverlayWindow.SetGizmoOwner(bone.UniqueName));
			if (show) {
				foreach (var child in children)
					DrawBoneTreeNode(child);
				ImGui.TreePop();
			}
		}

		private static bool DrawBoneNode(Bone bone, ImGuiTreeNodeFlags flag, System.Action? executeIfClicked = null) {
			bool isAncester = bone.GetDescendants().Any(b => b.UniqueId == $"{Skeleton.BoneSelect.Partial}_{Skeleton.BoneSelect.Index}");
			if (isAncester) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
			bool show = ImGui.TreeNodeEx(bone.UniqueId, flag, bone.LocaleName);
			if (isAncester) ImGui.PopStyleColor();

			var rectMin = ImGui.GetItemRectMin() + new Vector2(ImGui.GetTreeNodeToLabelSpacing(), 0);
			var rectMax = ImGui.GetItemRectMax();

			var mousePos = ImGui.GetMousePos();
			if (
				ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& mousePos.X > rectMin.X && mousePos.X < rectMax.X
				&& mousePos.Y > rectMin.Y && mousePos.Y < rectMax.Y
			) {
				executeIfClicked?.Invoke();
			}
			return show;
		}
	}
}