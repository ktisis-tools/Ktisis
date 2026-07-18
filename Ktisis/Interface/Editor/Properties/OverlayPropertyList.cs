using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.KTK;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Utility;

using Lumina.Excel.Sheets;

namespace Ktisis.Interface.Editor.Properties;

public record StatusRow {
	public uint Icon = 0;
	public string Name = string.Empty;
	public string Path = string.Empty;
}

public class OverlayPropertyList : ObjectPropertyList {
	private readonly IDataManager _data;
	private readonly ITextureProvider _texture;
	private readonly IEditorContext _ctx;
	private readonly LocaleManager _locale;
	private readonly List<StatusRow> _statuses;
	private readonly PopupList<StatusRow> _statusPopup;

	public OverlayPropertyList(
		IDataManager data,
		ITextureProvider texture,
		IEditorContext ctx,
		LocaleManager locale
	) {
		this._data = data;
		this._texture = texture;
		this._ctx = ctx;
		this._locale = locale;
		this._statuses = new List<StatusRow>();

		foreach (var status in this._data.GetExcelSheet<Status>()) {
			if (!status.Name.IsEmpty && status.Icon != 0 && this._statuses.All(statusRow => statusRow.Icon != status.Icon)) {
				try {
					this._statuses.Add(new StatusRow() {
						Icon = status.Icon,
						Name = status.Name.ExtractText(),
						Path = this._texture.GetIconPath(status.Icon)
					});
				} catch (FileNotFoundException e) {
					Ktisis.Log.Verbose(e.ToString());
				}
			}
		}

		this._statusPopup = new PopupList<StatusRow>(
			"##StatusPopup",
			this.DrawStatusRow
		).WithSearch(StatusSearchPredicate);
	}

	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not OverlayEntity overlay)
			return;

		builder.AddHeader(Ktisis.Locale.Translate("object_edit.overlay.header"), () => this.DrawOverlayTab(overlay));
	}

	private void DrawOverlayTab(OverlayEntity overlay) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.Text(Ktisis.Locale.Translate("object_edit.overlay.pos"));
		ImGui.Spacing();

		using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), overlay.Draggable))
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.HandSpock, Ktisis.Locale.Translate("object_edit.overlay.pos_drag")))
				overlay.Draggable = !overlay.Draggable;

		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.ArrowsToDot, Ktisis.Locale.Translate("object_edit.overlay.pos_snap")))
			overlay.Position = GetCenter(overlay);

		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		var position = overlay.Position;
		if (ImGui.DragFloat2("##OverlayPosition", ref position))
			overlay.Position = position;

		ImGui.Spacing();
		ImGui.Text(Ktisis.Locale.Translate("object_edit.overlay.scale"));

		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.ArrowCircleLeft, Ktisis.Locale.Translate("object_edit.overlay.scale_reset")))
			overlay.Scale = 1.0f;

		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		var scale = overlay.Scale;
		if (ImGui.DragFloat("##OverlayScale", ref scale, 0.01f, 0f, 5.0f))
			overlay.Scale = scale;

		ImGui.Spacing();
		ImGui.Text(Ktisis.Locale.Translate("object_edit.overlay.alpha"));

		ImGui.Spacing();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		var alpha = overlay.Alpha / 255.0f;
		if (ImGui.SliderFloat("##OverlayAlpha", ref alpha, 0f, 1.0f))
			overlay.Alpha = alpha;

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		switch (overlay) {
			case TalkOverlay talk:
				this.DrawTalk(talk);
				break;
			case BalloonOverlay balloon:
				this.DrawBalloon(balloon);
				break;
			case StatusOverlay status:
				this.DrawStatus(status);
				break;
		}
	}

	private void DrawTalk(TalkOverlay talk) {
		var speaker = talk.Speaker;
		if (ImGui.InputText(Ktisis.Locale.Translate("object_edit.overlay.talk.speaker"), ref speaker, 64))
			talk.Speaker = speaker;

		ImGui.Spacing();
		var dialog = talk.Dialog;
		if (ImGui.InputTextMultiline(Ktisis.Locale.Translate("object_edit.overlay.talk.content"), ref dialog, 1000))
			talk.Dialog = dialog;

		ImGui.Spacing();
		var size = talk.FontSize;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.talk.fontsize"), size.ToString())) {
			foreach (var value in talk.FontSizes)
				if (ImGui.Selectable(value.ToString(), size == value))
					talk.FontSize = value;
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var background = talk.Background;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.talk.bg"), this._locale.Translate($"background.{background}"))) {
			foreach (var value in Enum.GetValues<TalkBackground>()) {
				var valueLabel = this._locale.Translate($"background.{value}");
				if (ImGui.Selectable(valueLabel, background == value))
					talk.Background = value;
			}
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var cursor = talk.Cursor;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.talk.cursor"), this._locale.Translate($"cursor.{cursor}"))) {
			foreach (var value in Enum.GetValues<TalkCursor>()) {
				var valueLabel = this._locale.Translate($"cursor.{value}");
				if (ImGui.Selectable(valueLabel, cursor == value))
					talk.Cursor = value;
			}
			ImGui.EndCombo();
		}
	}

	private void DrawBalloon(BalloonOverlay balloon) {
		var dialog = balloon.Dialog;
		if (ImGui.InputText(Ktisis.Locale.Translate("object_edit.overlay.balloon.content"), ref dialog, 64))
			balloon.Dialog = dialog;

		ImGui.Spacing();
		var size = balloon.FontSize;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.balloon.fontsize"), size.ToString())) {
			foreach (var value in balloon.FontSizes)
				if (ImGui.Selectable(value.ToString(), size == value))
					balloon.FontSize = value;
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var background = balloon.Background;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.balloon.bg"), this._locale.Translate($"background.{background}"))) {
			foreach (var value in Enum.GetValues<BalloonBackground>()) {
				var valueLabel = this._locale.Translate($"background.{value}");
				if (ImGui.Selectable(valueLabel, background == value))
					balloon.Background = value;
			}
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var color = balloon.Color;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.balloon.gradient"), this._locale.Translate($"gradient.{color}"))) {
			foreach (var value in Enum.GetValues<BalloonColor>()) {
				var valueLabel = this._locale.Translate($"gradient.{value}");
				if (ImGui.Selectable(valueLabel, color == value))
					balloon.Color = value;
			}
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var arrow = balloon.Arrow;
		if (ImGui.Checkbox(Ktisis.Locale.Translate("object_edit.overlay.balloon.arrow_show"), ref arrow))
			balloon.Arrow = arrow;

		ImGui.Spacing();
		using (ImRaii.Disabled(!balloon.Arrow)) {
			var arrowX = balloon.ArrowX;
			if (ImGui.SliderFloat(Ktisis.Locale.Translate("object_edit.overlay.balloon.arrow_pos"), ref arrowX, 32.0f, 130.0f))
				balloon.ArrowX = arrowX;
		}
	}

	private void DrawStatus(StatusOverlay status) {
		var text = status.StatusText;
		if (ImGui.InputText(Ktisis.Locale.Translate("object_edit.overlay.status.content"), ref text, 64))
			status.StatusText = text;

		ImGui.Spacing();
		var type = status.StatusType;
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.overlay.status.type"), this._locale.Translate($"status.{type}"))) {
			foreach (var value in Enum.GetValues<StatusType>()) {
				var valueLabel = this._locale.Translate($"status.{value}");
				if (ImGui.Selectable(valueLabel, type == value))
					status.StatusType = value;
			}
			ImGui.EndCombo();
		}

		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Image, Ktisis.Locale.Translate("object_edit.overlay.status.tex_hint")))
			this._statusPopup.Open();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		var currentStatus = this._statuses.FirstOrDefault(stat => stat.Path == status.IconPath);
		ImGui.Text($"{Ktisis.Locale.Translate("object_edit.overlay.status.tex")} {currentStatus?.Name}");
		if (currentStatus != null) {
			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			ImGui.Image(this._texture.GetFromGameIcon(currentStatus.Icon).GetWrapOrEmpty().Handle, new Vector2(24.0f, 32.0f));
		}

		this.DrawStatusPopup(status);
	}

	private unsafe void DrawStatusPopup(StatusOverlay status) {
		if (!this._statusPopup.IsOpen) return;
		if (!this._statusPopup.Draw(this._statuses, this._statuses.Count, out var selected, 32.0f)) return;

		status.IconPath = selected!.Path;
	}

	private bool DrawStatusRow(StatusRow status, bool isFocus) {
		var space = ImGui.GetStyle().ItemSpacing.X;
		var cursor = ImGui.GetCursorPosX();
		var result = ImGui.Button(string.Empty, new Vector2(ImGui.GetContentRegionAvail().X, 32.0f));
		ImGui.SameLine(cursor, 24.0f + space);
		ImGui.Text(status.Name);

		ImGui.SameLine(cursor);
		ImGui.Image(this._texture.GetFromGameIcon(status.Icon).GetWrapOrEmpty().Handle, new Vector2(24.0f, 32.0f));

		return result;
	}

	private static bool StatusSearchPredicate(StatusRow status, string query)
		=> status.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

	private static Vector2 GetCenter(OverlayEntity entity) {
		var screenSize = ImGui.GetMainViewport().Size;
		var screenCenter = screenSize / 2;

		var nodeSize = entity.Size * entity.Scale;
		var nodeCenter = nodeSize / 2;

		return new Vector2(screenCenter.X -  nodeCenter.X, screenCenter.Y -  nodeCenter.Y);
	}
}
