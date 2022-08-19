using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Overlay;

namespace Ktisis.Structs.Bones {
	public class BoneSelector {
		public (int ListId, int Index) Current = (-1, -1); // Find a better way of doing this

		public int ScrollIndex = 0;

		public void Draw(SkeletonEditor editor, List<(int ListId, int Index)> hover) {
			// Capture mouse input while hovering a bone.
			// This allows us to intercept mouse clicks.
			ImGui.SetNextFrameWantCaptureMouse(true);

			var pos = ImGui.GetMousePos();
			ImGui.SetNextWindowPos(pos + new Vector2(20, 0));

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Bone Selector", ImGuiWindowFlags.NoDecoration)) {
				var mouseDown = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
				var mouseWheel = ImGui.GetIO().MouseWheel;

				ScrollIndex = ScrollIndex - (int)mouseWheel;
				if (ScrollIndex >= hover.Count)
					ScrollIndex = 0;
				else if (ScrollIndex < 0)
					ScrollIndex = hover.Count - 1;

				for (int i = 0; i < hover.Count; i++) {
					var item = hover[i];

					var bones = editor.Skeleton?[item.ListId];
					if (bones == null) continue;

					var bone = bones?[item.Index];
					if (bone == null) continue;

					var name = editor.Plugin.Locale.GetBoneName(bone.HkaBone.Name!);
					var isSelected = i == ScrollIndex;
					ImGui.Selectable(name, isSelected);

					if (isSelected && mouseDown)
						editor.SelectBone(bone);
				}
			}

			ImGui.PopStyleVar(1);

			ImGui.End();
		}

		public bool IsSelected(Bone bone) {
			return (bone.BoneList.Id, bone.Index) == Current;
		}
	}
}
