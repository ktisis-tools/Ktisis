using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Editor.Properties;

public class ObjectEntityPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;

	public ObjectEntityPropertyList(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}

	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not ObjectEntity obj) return;
		builder.AddHeader(Ktisis.Locale.Translate("object_edit.object.header"), () => this.DrawTab(obj), priority: -1);
	}

	private void DrawTab(ObjectEntity obj) {
		var name = obj.Name;
		if (ImGui.InputText(Ktisis.Locale.Translate("object_edit.object.rename"), ref name, 100))
			obj.Name = name;

		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.object.title"), textColor:ImGui.GetColorU32(ImGuiCol.Header));

		// diagnostics
		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Clipboard, Ktisis.Locale.Translate("object_edit.object.copy_path")))
			ImGui.SetClipboardText(obj.GetPath());
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.Text($"{Ktisis.Locale.Translate("object_edit.object.path")}: {obj.GetPath()}");

		ImGui.Spacing();
		ImGui.Text($"{Ktisis.Locale.Translate("object_edit.object.type")}: {(obj.IsWorldObject() ? Ktisis.Locale.Translate("object_edit.object.type.world") : Ktisis.Locale.Translate("object_edit.object.type.spawned"))}");
		using (ImRaii.Disabled())
			ImGui.Text($"{Ktisis.Locale.Translate("object_edit.object.addr")}: {obj.Address:X}");

		if (obj.CanStain()) {
			Vector4 existingColor = obj.GetStain();
			if (ImGui.ColorEdit4("Stain", ref existingColor)) {
				var srgbColor = new Vector4(MathF.Sqrt(existingColor.X), MathF.Sqrt(existingColor.Y), MathF.Sqrt(existingColor.Z), existingColor.Z) * byte.MaxValue;
				var byteColor = new ByteColor() { R = (byte)srgbColor.X, G = (byte)srgbColor.Y, B = (byte)srgbColor.Z, A = (byte)srgbColor.W };
				obj.TrySetStainColor(byteColor);

			}
		}

		byte curVal = obj.ColorIntensity;
		Vector4 curCol = obj.Color;
		if (ImGui.DragByte(Ktisis.Locale.Translate("object_edit.object.intensity"), ref curVal, vMax: 32)) {
			obj.ColorIntensity = curVal;
		}
		if (ImGui.ColorEdit4(Ktisis.Locale.Translate("object_edit.object.tint"), ref curCol)) {
			obj.Color = curCol;
		}
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(Ktisis.Locale.Translate(Ktisis.Locale.Translate("object_edit.object.intensityHint")));
		}
		
		// buttons
		ImGui.Spacing();
		if (obj.IsWorldObject()) {
			if (ImGui.Button(Ktisis.Locale.Translate("workspace.entity_menu.base.reset")))
				obj.Reset();

			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			if (ImGui.Button(Ktisis.Locale.Translate("workspace.entity_menu.base.untrack")))
				obj.Remove();
		} else {
			if (ImGui.Button(Ktisis.Locale.Translate("workspace.entity_menu.base.delete")))
				obj.Remove();
		}
	}
}
