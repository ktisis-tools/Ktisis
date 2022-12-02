using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class TransformWindow {
		private static bool Visible = false;
		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		public static TransformTable Transform = new();

		// Draw window
		public unsafe static void Draw() {
			if (!Visible || !Ktisis.IsInGPose)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * 16, 1), new Vector2(50000, 50000));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Transform", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				var target = Ktisis.GPoseTarget;
				var actor = (Actor*)target!.Address;

				if (actor->Model != null)
					TransformTable(actor);
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}

		// Transform Table actor and bone names display, actor related extra
		private unsafe static bool TransformTable(Actor* target) {
			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			if (!select.Active) return Transform.Draw(target);
			return bone != null && Transform.Draw(bone);

		}
	}

}