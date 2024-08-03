using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Entities.Utility;

namespace Ktisis.Interface.Overlay;

[Transient]
public class RefOverlay {
	private readonly ITextureProvider _tex;
	
	public RefOverlay(
		ITextureProvider tex
	) {
		this._tex = tex;
	}
	
	public void DrawInstance(ReferenceImage image) {
		var open = image.Visible;
		if (!open) return;
		
		var texture = this._tex.GetFromFile(image.FilePath);
		if (!texture.TryGetWrap(out var wrap, out var err)) return;
		
		ImGui.SetNextWindowSize(wrap.Size, ImGuiCond.FirstUseEver);
		HandleImageAspectRatio(wrap.Size);

		using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		var id = $"{image.Name}##{image.GetHashCode():X}";
		
		const ImGuiWindowFlags flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings;
		if (!ImGui.Begin(id, ref open, flags)) return;
		
		try {
			var avail = ImGui.GetContentRegionAvail();
			var tintColor = Vector4.One with { W = image.Opacity };
			ImGui.Image(wrap.ImGuiHandle, avail, Vector2.Zero, Vector2.One, tintColor);
			this.HandlePopup(id, avail, image);
		} finally {
			ImGui.End();
		}

		if (!open) image.Visible = false;
	}

	private void HandlePopup(string id, Vector2 avail, ReferenceImage image) {
		var popupId = $"{id}##popup";
		
		var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Right)
			|| (ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));
		
		if (clicked) {
			ImGui.OpenPopup(popupId);
			ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos());
		}

		using var popup = ImRaii.Popup(popupId);
		if (!popup.Success) return;
		
		ImGui.SetNextItemWidth(avail.X);
		ImGui.SliderFloat("##ref_opacity", ref image.Opacity, 0.0f, 1.0f);
	}

	private unsafe static void HandleImageAspectRatio(Vector2 size) {
		if (size.X == 0.0f || size.Y == 0.0f) return;
		
		var ratio = size.X / size.Y;
		
		var screen = ImGui.GetIO().DisplaySize * 0.9f;
		var max = new Vector2(screen.Y * ratio, screen.X / ratio);
		var min = size * 0.10f;
		
		var height = ImGui.GetFrameHeight();
		
		ImGui.SetNextWindowSizeConstraints(min, max, data => {
			if (data == null) return;
			var x = data->DesiredSize.X / ratio;
			data->DesiredSize.Y = height + x;
		});
	}
}
