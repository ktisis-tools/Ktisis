using System.Linq;
using System.Numerics;

using ImGuiNET;

using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Components {
	public class BoneTree {
		private static Vector2 _FrameMin;
		private static Vector2 _FrameMax;

		public static unsafe void Draw(Actor* actor) {
			if (ImGui.CollapsingHeader("Bone List")) {
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
		private unsafe static void DrawBoneTreeNode(Bone bone) {
			if (bone == null) return;

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
			if (bone == null) return false;

			bool isAncester = bone.GetDescendants().Any(b => b.UniqueId == $"{Skeleton.BoneSelect.Partial}_{Skeleton.BoneSelect.Index}");
			if (isAncester) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
			bool show = ImGui.TreeNodeEx(bone.UniqueId, flag, bone.LocaleName);
			if (isAncester) ImGui.PopStyleColor();

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
	}
}