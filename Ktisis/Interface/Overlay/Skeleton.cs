using System;

using ImGuiNET;

using Ktisis.Services;
using Ktisis.Scene.Skeletons;
using Ktisis.Library.Extensions;

namespace Ktisis.Interface.Overlay {
	public static class Skeleton {
		public static void Toggle() {
			var visible = !Ktisis.Configuration.ShowOverlay;
			Ktisis.Configuration.ShowOverlay = visible;
		}

		public unsafe static void Draw(SkeletonObject skele) {
			var skelePtr = skele.GetSkeleton();
			if (skelePtr == null) return;

			var model = skele.GetObject();
			if (model == null) return;

			var world = Interop.Methods.GetMatrix();
			if (world == null) return;

			var draw = ImGui.GetWindowDrawList();

			foreach (var (pair, manip) in skele.BoneMap) {
				if (!manip.ShouldDraw()) continue;

				var bone = skelePtr->GetBone(pair.p, pair.i);
				if (bone == null || !bone.IsValid())
					continue;

				var isVisible = world->WorldToScreen(bone.GetWorldPos(model), out var pos2d);

				// Draw line to bone parent if any
				if (bone.ParentId > 0) {
					// TODO: Draw lines for parents of partials.

					(int p, int i) parentPair = (pair.p, bone.ParentId);
					if (skele.BoneMap.TryGetValue(parentPair, out var parentManip) && parentManip.ShouldDraw()) {
						var parent = skelePtr->GetBone(parentPair.p, parentPair.i);
						if (parent != null) {
							var lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / DalamudServices.Camera->Camera->InterpDistance * 2f);
							isVisible &= world->WorldToScreen(parent.GetWorldPos(model), out var parentPos2d);
							if (isVisible)
								draw.AddLine(pos2d, parentPos2d, 0xFFFFFFFF, lineThickness);
						}
					}
				}

				// Create selectable item
				if (isVisible) {
					var item = Selection.AddItem(manip.Name, pos2d, 0xFFFFFFFF);
					if (item != null && item.IsClicked())
						manip.UiSelect();
				}
			}
		}
	}
}