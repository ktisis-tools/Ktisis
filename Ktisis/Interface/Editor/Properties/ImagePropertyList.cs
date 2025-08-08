using System.IO;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Utility;

namespace Ktisis.Interface.Editor.Properties;

public class ImagePropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	
	public ImagePropertyList(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not ReferenceImage img)
			return;
		
		builder.AddHeader("Reference Image", () => this.DrawImageTab(img));
	}

	private void DrawImageTab(ReferenceImage img) {
		const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.ReadOnly;
		
		var path = Path.GetFileName(img.Data.FilePath);
		ImGui.InputText("##RefImgPath", ref path, flags: inputFlags);
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, "Load image", new Vector2(0, ImGui.GetFrameHeight())))
			this._ctx.Interface.OpenReferenceImages(img.SetFilePath);

		ImGui.SliderFloat("Opacity##RefImgOpacity", ref img.Data.Opacity, 0.0f, 1.0f);
	}
}
