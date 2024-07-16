using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Config.Pose2D;
using Ktisis.Interface.Components.Posing.Types;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Components.Posing;

public class PoseViewRenderer {
	private readonly ITextureProvider _tex;

	private readonly Dictionary<string, ISharedImmediateTexture> Textures = new();

	public PoseViewRenderer(
		ITextureProvider tex
	) {
		this._tex = tex;
	}

	public IViewFrame StartFrame() {
		return new ViewFrame(this);
	}
	
	// Images
	
	private ISharedImmediateTexture GetTexture(string file) {
		if (this.Textures.TryGetValue(file, out var texture))
			return texture;
		
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;
		texture = this._tex.GetFromManifestResource(assembly, $"{name}.Data.Images.{file}");
		this.Textures.Add(file, texture);
		return texture;
	}
	
	// ViewFrame
	
	private class ViewFrame : IViewFrame {
		private readonly PoseViewRenderer _render;
		
		private readonly List<ViewData> Views = new();

		public ViewFrame(
			PoseViewRenderer render
		) {
			this._render = render;
		}
		
		public void DrawView(PoseViewEntry entry, float width = 1.0f, float height = 1.0f) {
			var file = entry.Images.First();
			var img = this._render.GetTexture(file).GetWrapOrDefault();
			if (img == null) return;

			var avail = ImGui.GetWindowContentRegionMax();
			avail.X -= ImGui.GetStyle().ItemSpacing.X * (this.Views.Count + 1);
			
			var min = ImGui.GetCursorScreenPos();
			var scale = Math.Min(
				(avail.X * width) / img.Size.X,
				(avail.Y * height) / img.Size.Y
			);
			var size = img.Size * scale;

			ImGui.Image(img.ImGuiHandle, size);
			
			this.Views.Add(new ViewData {
				Entry = entry,
				ScreenPos = min,
				Size = size
			});
		}

		public void DrawBones(EntityPose pose) {
			var draw = ImGui.GetWindowDrawList();
			
			var isWinHover = ImGui.IsWindowHovered();
			BoneNode? hovered = null;
			
			foreach (var view in this.Views) {
				var isViewHover = isWinHover && ImGui.IsMouseHoveringRect(view.ScreenPos, view.ScreenPos + view.Size);
				
				foreach (var boneInfo in view.Entry.Bones) {
					var bone = pose.FindBoneByName(boneInfo.Name);
					if (bone == null) continue;
					
					var offset = view.Size * boneInfo.Position;
					var pos = view.ScreenPos + offset;
					
					var radius = MathF.Min(9.0f, view.Size.X * 0.04f);
					var radiusVec = new Vector2(radius, radius);
					
					var isHover = isViewHover && hovered == null && ImGui.IsMouseHoveringRect(pos - radiusVec, pos + radiusVec);
					draw.AddCircleFilled(pos, radius, (isHover || bone.IsSelected) ? 0xFFFFFFFF : 0x70DFDFDF, 64);
					draw.AddCircle(pos, radius, 0xFF000000, 64, isHover ? 2.5f : 2f);
					if (isHover) hovered = bone;
				}
			}

			if (hovered != null) {
				var fore = ImGui.GetForegroundDrawList();
				
				var pad = new Vector2(5, 5);
				var pos = ImGui.GetMousePos() + new Vector2(20, 0);
				fore.AddRectFilled(pos - pad, pos + ImGui.CalcTextSize(hovered.Name) + pad, 0xFF000000, 5f);
				fore.AddText(pos, 0xFFFFFFFF, hovered.Name);
				if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
					var mode = GuiHelpers.GetSelectMode();
					hovered.Select(mode);
				}
			}
		}
	}
	
	// ViewData
	
	private record ViewData {
		public required PoseViewEntry Entry;
		public required Vector2 ScreenPos;
		public required Vector2 Size;
	}
}
