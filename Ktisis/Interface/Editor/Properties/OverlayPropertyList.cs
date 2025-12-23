using System;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.KTK;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Utility;

namespace Ktisis.Interface.Editor.Properties;

public class OverlayPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly LocaleManager _locale;

	public OverlayPropertyList(
		IEditorContext ctx,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._locale = locale;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not OverlayEntity overlay)
			return;
		
		builder.AddHeader("Dialog", () => this.DrawTalkTab(overlay));
	}

	private void DrawTalkTab(OverlayEntity overlay) {
		var position = overlay.Position;
		if (ImGui.DragFloat2("Position", ref position))
			overlay.Position = position;
		
		ImGui.Spacing();
		var movable = overlay.Draggable;
		if (ImGui.Checkbox("Movable", ref movable))
			overlay.Draggable = movable;

		if (overlay is TalkOverlay talk) {
			
			ImGui.Spacing();
			var speaker = talk.Speaker;
			if (ImGui.InputText("Speaker", ref speaker, 64))
				talk.Speaker = speaker;
		
			ImGui.Spacing();
			var dialog = talk.Dialog;
			if (ImGui.InputTextMultiline("Dialog", ref dialog, 1000))
				talk.Dialog = dialog;
		
			ImGui.Spacing();
			var background = talk.Background;
			if (ImGui.BeginCombo("Background", this._locale.Translate($"background.{background}"))) {
				foreach (var value in Enum.GetValues<TalkBackground>()) {
					var valueLabel = this._locale.Translate($"background.{value}");
					if (ImGui.Selectable(valueLabel, background == value))
						talk.Background = value;
				}
				ImGui.EndCombo();
			}
		
			ImGui.Spacing();
			var cursor = talk.Cursor;
			if (ImGui.BeginCombo("Cursor", this._locale.Translate($"cursor.{cursor}"))) {
				foreach (var value in Enum.GetValues<TalkCursor>()) {
					var valueLabel = this._locale.Translate($"cursor.{value}");
					if (ImGui.Selectable(valueLabel, cursor == value))
						talk.Cursor = value;
				}
				ImGui.EndCombo();
			}
		}
		else if (overlay is BalloonOverlay balloon) {
			ImGui.Spacing();
			var dialog = balloon.Dialog;
			if (ImGui.InputText("Dialog", ref dialog, 64))
				balloon.Dialog = dialog;

			ImGui.Spacing();
			var background = balloon.Background;
			if (ImGui.BeginCombo("Background", this._locale.Translate($"background.{background}"))) {
				foreach (var value in Enum.GetValues<BalloonBackground>()) {
					var valueLabel = this._locale.Translate($"background.{value}");
					if (ImGui.Selectable(valueLabel, background == value))
						balloon.Background = value;
				}
				ImGui.EndCombo();
			}
		}
	}
}
