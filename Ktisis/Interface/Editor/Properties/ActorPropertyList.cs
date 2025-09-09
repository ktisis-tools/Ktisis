using Dalamud.Interface;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.Windows.Import;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Editor.Properties;

public class ActorPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LocaleManager _locale;
	
	public ActorPropertyList(
		IEditorContext ctx,
		GuiManager gui,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._gui = gui;
		this._locale = locale;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (
			entity switch {
				BoneNode node => node.Pose.Parent,
				BoneNodeGroup group => group.Pose.Parent,
				EntityPose pose => pose.Parent,
				_ => entity
			} is not ActorEntity actor
		) return;

		builder.AddHeader("Actor", () => this.DrawActorTab(actor), priority: 0);
	}
	
	// Actor tab

	private const string ImportOptsPopupId = "##KtisisCharaImportOptions";

	private void DrawActorTab(ActorEntity actor) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Position lock
		
		var posLock = this._ctx.Animation.PositionLockEnabled;
		if (ImGui.Checkbox(this._locale.Translate("actors.pos_lock"), ref posLock))
			this._ctx.Animation.PositionLockEnabled = posLock;
		
		ImGui.Spacing();
		
		// Open appearance editor

		if (Buttons.IconButton(FontAwesomeIcon.Edit))
			this._ctx.Interface.OpenActorEditor(actor);
		ImGui.SameLine(0, spacing);
		ImGui.Text("Actor Editor");
		
		ImGui.Spacing();
		
		// Import/export

		// if (ImGui.Button("Import"))
		// 	this._ctx.Interface.OpenCharaImport(actor);
		// ImGui.SameLine(0, spacing);
		if (ImGui.Button("Export Chara"))
			this._ctx.Interface.OpenCharaExport(actor);

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.Text("Import actor appearance...");
		ImGui.Spacing();

		var embedEditor = this._gui.GetOrCreate<CharaImportDialog>(this._ctx);
		embedEditor.OnOpen();
		embedEditor.SetTarget(actor);
		embedEditor.DrawEmbed();
	}
	
	// Gaze tab

	private void DrawGazeTab() {
		
	}
}
