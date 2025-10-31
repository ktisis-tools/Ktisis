using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using FFXIVClientStructs.FFXIV.Component.GUI;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Config;
using Ktisis.Interface.Types;
using Ktisis.Services.Data;
using Ktisis.Localization;

namespace Ktisis.Interface.Windows;

public class ConfigWindow : KtisisWindow {
	private readonly ConfigManager _cfg;
	private readonly GuiManager _gui;
	private readonly ContextManager _context;
	
	private readonly FormatService _format;

	private readonly ActionKeybindEditor _keybinds;
	private readonly BoneCategoryEditor _boneCategories;
	private readonly GizmoStyleEditor _gizmoStyle;
	private readonly PresetEditor _presetEditor;
	private readonly OffsetEditor _offsetEditor;
	public readonly LocaleManager Locale;

	private Configuration Config => this._cfg.File;

	const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.ReadOnly;
	private const ImGuiTreeNodeFlags treeFlags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow;
	private const ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Leaf;

	// ty OGT https://git.anna.lgbt/anna/OrangeGuidanceTomestone/src/branch/main/client/Ui/MainWindowTabs/Settings.cs#L23
	private delegate void DrawContentDelegate();
	private IReadOnlyList<(string, DrawContentDelegate)> Tabs { get; }
	private int _tabIndex = 0;

	public ConfigWindow(
		ConfigManager cfg,
		ContextManager context,
		FormatService format,
		ActionKeybindEditor keybinds,
		BoneCategoryEditor boneCategories,
		GizmoStyleEditor gizmoStyle,
		PresetEditor presetEditor,
		OffsetEditor offsetEditor,
		LocaleManager locale,
		GuiManager gui
	) : base("Ktisis Settings", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
		this._cfg = cfg;
		this._context = context;
		this._format = format;
		this._keybinds = keybinds;
		this._boneCategories = boneCategories;
		this._gizmoStyle = gizmoStyle;
		this._presetEditor = presetEditor;
		this._offsetEditor = offsetEditor;
		this.Locale = locale;
		this._gui = gui;

		this.Tabs = [
			(this.Locale.Translate("config.workspace.title"), this.DrawWorkspaceTab),
			(this.Locale.Translate("config.presets.title"), this.DrawPresetsTab),
			(this.Locale.Translate("config.autosave.title"), this.DrawAutoSaveTab),
			(this.Locale.Translate("config.categories.title"), this.DrawCategoriesTab),
			(this.Locale.Translate("config.gizmo.title"), this.DrawGizmoTab),
			(this.Locale.Translate("config.overlay.title"), this.DrawOverlayTab),
			(this.Locale.Translate("config.offsets.title"), this.DrawOffsetsTab),
			(this.Locale.Translate("config.poseview.title"), this.DrawPoseViewTab),
			(this.Locale.Translate("config.input.title"), this.DrawInputTab),
			(this.Locale.Translate("config.input.cameras.title"), this.DrawCamerasInputTab),
			(this.Locale.Translate("config.input.gizmo.title"), this.DrawGizmoInputTab),
			(this.Locale.Translate("config.about.title"), this.DrawAboutTab)
		];
	}
	
	// Open

	public override void OnOpen() {
		this._keybinds.Setup();
		this._boneCategories.Setup();
		this._presetEditor.Setup();
		this._offsetEditor.Setup();
	}
	
	// Draw

	private void DrawTabNode(int index, List<int>? children = null) {
		var hasChildren = children != null;
		var flags = hasChildren ? treeFlags : leafFlags;
		if (this._tabIndex == index) flags |= ImGuiTreeNodeFlags.Selected;

		using var _tab = ImRaii.TreeNode(this.Tabs[index].Item1, flags);
		if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
			this._tabIndex = index;
		if (hasChildren && _tab.Success)
			foreach (var childIndex in children!)
				this.DrawTabNode(childIndex);
	}

	public override void Draw() {
		using var _table = ImRaii.Table("##ConfigTable", 2);
		if (!_table.Success) return;
		ImGui.TableSetupColumn("##tabs", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
		ImGui.TableSetupColumn("##content", ImGuiTableColumnFlags.NoResize);
		ImGui.TableNextRow();

		ImGui.TableNextColumn();
		this.DrawTabNode(0, [1, 2]);
		this.DrawTabNode(3, [4, 5, 6, 7]);
		this.DrawTabNode(8, [9, 10]);
		this.DrawTabNode(11);

		ImGui.TableNextColumn();
		var avail = ImGui.GetContentRegionAvail();
		using var _frame = ImRaii.Child("##ConfigContent", avail, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize);
		var (_, drawFn) = this.Tabs[this._tabIndex];
		drawFn();
	}

	private void DrawHint(string localeHandle) {
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(this.Locale.Translate(localeHandle));
		}
	}
	
	// Tabs
	
	// Categories

	private void DrawCategoriesTab() {
		var refresh = false;
		ImGui.Text(this.Locale.Translate("config.categories.header"));
		ImGui.Spacing();

		refresh |= ImGui.Checkbox(this.Locale.Translate("config.categories.show_all_viera_ears"), ref this.Config.Categories.ShowAllVieraEars);
		this.DrawHint("config.categories.hint_viera_ears");
		ImGui.Checkbox(this.Locale.Translate("config.categories.show_friendly_bone_names"), ref this.Config.Categories.ShowFriendlyBoneNames);
		this.DrawHint("config.categories.hint_friendly_bones");
		if (refresh) this.RefreshScene();

		ImGui.Spacing();
		this._boneCategories.Draw();
	}
	
	// Gizmo

	private void DrawGizmoTab() {
		ImGui.Text(this.Locale.Translate("config.gizmo.header"));
		ImGui.Spacing();

		ImGui.Checkbox(this.Locale.Translate("config.gizmo.flip"), ref this.Config.Gizmo.AllowAxisFlip);
		ImGui.Checkbox(this.Locale.Translate("config.gizmo.raySnap"), ref this.Config.Gizmo.AllowRaySnap);

		ImGui.Spacing();
		this._gizmoStyle.Draw();
	}
	
	// Overlay

	private void DrawOverlayTab() {
		ImGui.Text(this.Locale.Translate("config.overlay.header"));
		ImGui.Spacing();

		ImGui.Checkbox(this.Locale.Translate("config.overlay.lines.draw"), ref this.Config.Overlay.DrawLines);
		ImGui.Checkbox(this.Locale.Translate("config.overlay.lines.draw_gizmo"), ref this.Config.Overlay.DrawLinesGizmo);
		ImGui.Checkbox(this.Locale.Translate("config.overlay.dots.draw_gizmo"), ref this.Config.Overlay.DrawDotsGizmo);
		ImGui.Spacing();
		ImGui.Checkbox(this.Locale.Translate("config.references.draw_title"), ref this.Config.Overlay.DrawReferenceTitle);
		ImGui.Spacing();
		ImGui.DragFloat(this.Locale.Translate("config.overlay.dots.radius"), ref this.Config.Overlay.DotRadius, 0.1f);
		ImGui.DragFloat(this.Locale.Translate("config.overlay.lines.thick"), ref this.Config.Overlay.LineThickness, 0.1f);
		ImGui.Spacing();
		ImGui.SliderFloat(this.Locale.Translate("config.overlay.lines.opacity"), ref this.Config.Overlay.LineOpacity, 0.0f, 1.0f);
		ImGui.SliderFloat(this.Locale.Translate("config.overlay.lines.opacity_gizmo"), ref this.Config.Overlay.LineOpacityUsing, 0.0f, 1.0f);
	}
	
	// Workspace
	
	private void DrawWorkspaceTab() {
		ImGui.Text(this.Locale.Translate("config.workspace.header"));
		ImGui.Spacing();

		ImGui.Checkbox(this.Locale.Translate("config.workspace.init"), ref this.Config.Editor.OpenOnEnterGPose);
		this.DrawHint("config.workspace.hintInit");
		ImGui.Checkbox(this.Locale.Translate("config.workspace.incognitoPlayerNames"), ref this.Config.Editor.IncognitoPlayerNames);
		this.DrawHint("config.workspace.hintIncognito");
		var refresh = ImGui.Checkbox(this.Locale.Translate("config.categories.allow_nsfw"), ref this.Config.Categories.ShowNsfwBones);
		this.DrawHint("config.categories.hint_nsfw");
		ImGui.Checkbox(this.Locale.Translate("config.workspace.confirmExit"), ref this.Config.Editor.ConfirmExit);
		ImGui.Checkbox(this.Locale.Translate("config.workspace.initPosLock"), ref this.Config.Editor.InitPosLock);
		this.DrawHint("config.workspace.hintPosLock");
		ImGui.Spacing();

		if (ImGui.CollapsingHeader(this.Locale.Translate("config.workspace.windowHeader"))) {
			ImGui.Checkbox(this.Locale.Translate("config.workspace.toggleOpenWindows"), ref this.Config.Editor.ToggleOpenWindows);
			ImGui.Checkbox(this.Locale.Translate("config.workspace.editOnSelect"), ref this.Config.Editor.ToggleEditorOnSelect);
			using (ImRaii.Disabled(!this.Config.Editor.ToggleEditorOnSelect))
				ImGui.Checkbox(this.Locale.Translate("config.workspace.closeOnDeselect"), ref this.Config.Editor.CloseEditorOnDeselect);
		}
		ImGui.Spacing();

		if (ImGui.CollapsingHeader(this.Locale.Translate("config.workspace.legacyHeader"))) {
			ImGui.Checkbox(this.Locale.Translate("config.workspace.legacyWindows"), ref this.Config.Editor.UseLegacyWindowBehavior);
			this.DrawHint("config.workspace.legacyWindowHint");
			ImGui.Checkbox(this.Locale.Translate("config.workspace.legacyPoseTabs"), ref this.Config.Editor.UseLegacyPoseViewTabs);
			this.DrawHint("config.workspace.legacyPoseHint");
			ImGui.Checkbox(this.Locale.Translate("config.workspace.legacyLightEditor"), ref this.Config.Editor.UseLegacyLightEditor);
			this.DrawHint("config.workspace.legacyLightHint");
		}
		ImGui.Spacing();

		if (refresh) this.RefreshScene();
	}

	// Input

	private void DrawInputTab() {
		ImGui.Checkbox(this.Locale.Translate("config.input.enable"), ref this.Config.Keybinds.Enabled);
		ImGui.Checkbox(this.Locale.Translate("config.input.blockLeft"), ref this.Config.Keybinds.BlockTargetLeftClick);
		ImGui.Checkbox(this.Locale.Translate("config.input.blockRight"), ref this.Config.Keybinds.BlockTargetRightClick);
		ImGui.Spacing();

		ImGui.Text(this.Locale.Translate("config.input.help"));
		using var _disable = ImRaii.Disabled(!this.Config.Keybinds.Enabled);
		this._keybinds.Draw("history|select|overlay");
	}

	private void DrawCamerasInputTab() {
		ImGui.Text(this.Locale.Translate("config.input.cameras.header"));
		ImGui.Spacing();

		ImGui.DragFloat(this.Locale.Translate("config.workspace.workcam.speed"), ref this.Config.Editor.WorkcamMoveSpeed, 0.001f, 0.0f, 100.0f);
		ImGui.DragFloat(this.Locale.Translate("config.workspace.workcam.fastMulti"), ref this.Config.Editor.WorkcamFastMulti, 0.001f, 0.0f, 100.0f);
		ImGui.DragFloat(this.Locale.Translate("config.workspace.workcam.slowMulti"), ref this.Config.Editor.WorkcamSlowMulti, 0.001f, 0.0f, 100.0f);
		ImGui.DragFloat(this.Locale.Translate("config.workspace.workcam.vertMulti"), ref this.Config.Editor.WorkcamVertMulti, 0.001f, 0.0f, 100.0f);
		ImGui.DragFloat(this.Locale.Translate("config.workspace.workcam.sens"), ref this.Config.Editor.WorkcamSens, 0.001f, 0.0f, 100.0f);
		ImGui.Spacing();

		ImGui.Text(this.Locale.Translate("config.input.help"));
		using var _disable = ImRaii.Disabled(!this.Config.Keybinds.Enabled);
		this._keybinds.Draw("camera");
	}

	private void DrawGizmoInputTab() {
		ImGui.Text(this.Locale.Translate("config.input.gizmo.header"));
		ImGui.Spacing();

		ImGui.Text(this.Locale.Translate("config.input.help"));
		using var _disable = ImRaii.Disabled(!this.Config.Keybinds.Enabled);
		this._keybinds.Draw("gizmo");
	}
	
	
	// AutoSave

	private void DrawAutoSaveTab() {
		var cfg = this.Config.AutoSave;

		ImGui.Checkbox(this.Locale.Translate("config.autosave.enable"), ref cfg.Enabled);
		using (var _ = ImRaii.Disabled(!cfg.Enabled)) {
			ImGui.Checkbox(this.Locale.Translate("config.autosave.disconnect"), ref cfg.OnDisconnect);
			ImGui.Checkbox(this.Locale.Translate("config.autosave.ondisable"), ref cfg.OnDisable);
			ImGui.Checkbox(this.Locale.Translate("config.autosave.clear"), ref cfg.ClearOnExit);
		}
		
		ImGui.Spacing();

		ImGui.SliderInt(this.Locale.Translate("config.autosave.interval"), ref cfg.Interval, 10, 600, "%d s");
		ImGui.SliderInt(this.Locale.Translate("config.autosave.count"), ref cfg.Count, 1, 20);
		
		ImGui.Spacing();
		
		ImGui.InputText(this.Locale.Translate("config.autosave.path"), ref cfg.FilePath, 256);
		ImGui.InputText(this.Locale.Translate("config.autosave.dir"), ref cfg.FolderFormat, 256);
		
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled)))
			ImGui.TextUnformatted($"Example folder name: {this._format.Replace(cfg.FolderFormat)}");
		
		ImGui.Spacing();

		this.DrawAutoSaveFormatting();
	}

	private void DrawAutoSaveFormatting() {
		using var table = ImRaii.Table($"##AutoSaveFormatters", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.PadOuterX);
		if (!table.Success) return;

		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableSetupColumn("Formatter");
		ImGui.TableSetupColumn("Example Value");
		ImGui.TableHeadersRow();

		foreach (var (key, text) in this._format.GetReplacements()) {
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(key);
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(text);
		}
	}
	
	//Presets

	public void DrawPresetsTab() {
		var cfg = this.Config.Presets;

		this._presetEditor.Draw();
		
		var style = ImGui.GetStyle();
		var dummy = ImGui.GetContentRegionAvail() with { X = 0.0f };
		dummy.Y -= style.ItemSpacing.Y + style.CellPadding.Y;
		ImGui.Dummy(dummy);
	}

	public void DrawOffsetsTab() {
		var cfg = this.Config.Offsets;
		this._offsetEditor.Draw();

		var style = ImGui.GetStyle();
		var dummy = ImGui.GetContentRegionAvail() with { X = 0.0f };
		dummy.Y -= style.ItemSpacing.Y + style.CellPadding.Y;
		ImGui.Dummy(dummy);
	}

	public void DrawPoseViewTab() {
		var cfg = this.Config.PoseView;

		ImGui.Text(this.Locale.Translate("config.poseview.description"));

		// draw link to templates folder in git
		ImGui.AlignTextToFramePadding();
		ImGui.Text(this.Locale.Translate("config.poseview.linkout.description"));
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(this.Locale.Translate("config.poseview.linkout.button")))
			GuiHelpers.OpenBrowser(this.Locale.Translate("config.poseview.linkout.link"));

		// draw file selectors
		ImGui.Spacing();

		var loc = this.Locale.Translate("config.poseview.body");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.BodyPath = path);
			this.DrawPoseViewPath(ref cfg.BodyPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.armor");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.ArmorPath = path);
			this.DrawPoseViewPath(ref cfg.ArmorPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.face");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.FacePath = path);
			this.DrawPoseViewPath(ref cfg.FacePath, loc);
		}

		loc = this.Locale.Translate("config.poseview.lips");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.LipsPath = path);
			this.DrawPoseViewPath(ref cfg.LipsPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.mouth");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.MouthPath = path);
			this.DrawPoseViewPath(ref cfg.MouthPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.hands");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.HandsPath = path);
			this.DrawPoseViewPath(ref cfg.HandsPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.tail");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.TailPath = path);
			this.DrawPoseViewPath(ref cfg.TailPath, loc);
		}

		loc = this.Locale.Translate("config.poseview.ears");
		using (ImRaii.PushId($"poseview_{loc}")) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, $"Load {loc} Image"))
				this.SetPoseViewImage(path => cfg.EarsPath = path);
			this.DrawPoseViewPath(ref cfg.EarsPath, loc);
		}
	}

	private void DrawAboutTab() {
		ImGui.Text(this.Locale.Translate("config.about.header"));
		ImGui.Spacing();

		ImGui.TextWrapped(this.Locale.Translate("config.about.readme"));
		ImGui.Spacing();

		ImGui.AlignTextToFramePadding();
		ImGui.Text(this.Locale.Translate("config.about.discordLinkout.description"));
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(this.Locale.Translate("config.about.discordLinkout.button")))
			GuiHelpers.OpenBrowser(this.Locale.Translate("config.about.discordLinkout.link"));
		ImGui.Spacing();

		ImGui.AlignTextToFramePadding();
		ImGui.Text(this.Locale.Translate("config.about.gitLinkout.description"));
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(this.Locale.Translate("config.about.gitLinkout.button")))
			GuiHelpers.OpenBrowser(this.Locale.Translate("config.about.gitLinkout.link"));
	}

	// Handlers

	private void RefreshScene() => this._context.Current?.Scene.Refresh();

	private void DrawPoseViewPath(ref string configPath, string locale) {
		// draw remainder content after button and file dialog
		// note: filedialog has to be outside of child method due to ref nonsense
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.SameLine(0, spacing);
		using (ImRaii.Disabled(string.IsNullOrEmpty(configPath))) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Undo, "Reset"))
				configPath = null;
		}
		ImGui.SameLine(0, spacing);
		ImGui.InputText(locale, ref configPath, flags: inputFlags);
	}

	private void SetPoseViewImage(Action<string> handler) {
		this._gui.FileDialogs.OpenImage("image", handler);
	}

	public override void OnClose() {
		base.OnClose();
		this._cfg.Save();
	}
}
