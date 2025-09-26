using System;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Components.Workspace;

public class SceneDragDropHandler {
	private readonly IEditorContext _ctx;

	private IAttachManager Manager => this._ctx.Posing.Attachments;
	
	public SceneDragDropHandler(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}
	
	// Handling

	private const string PayloadId = "KTISIS_SCENE_NODE";

	private SceneEntity? Source;
	
	public void Handle(SceneEntity entity) {
		this.HandleSource(entity);
		if (this.Source != null)
			this.HandleTarget(entity);
	}

	private void HandleSource(SceneEntity entity) {
		if (entity is not IAttachable) return;
		using var src = ImRaii.DragDropSource(ImGuiDragDropFlags.SourceNoDisableHover);
		if (!src.Success) return;
		
		ImGui.SetDragDropPayload(PayloadId, ReadOnlySpan<byte>.Empty, 0);

		this.Source = entity;
		
		var display = this._ctx.Config.GetEntityDisplay(entity);
		using var color = ImRaii.PushColor(ImGuiCol.Text, display.Color);

		var icon = display.Icon;
		if (icon != FontAwesomeIcon.None) {
			Icons.DrawIcon(icon);
			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		}
		ImGui.Text(entity.Name);
	}

	private unsafe void HandleTarget(SceneEntity entity) {
		using var tar = ImRaii.DragDropTarget();
		if (!tar.Success) return;

		var pl = ImGui.AcceptDragDropPayload(PayloadId);		
		if (pl.Handle != null && this.Source is SceneEntity source)
			this.HandlePayload(entity, source);
	}

	private unsafe void HandlePayload(SceneEntity target, SceneEntity source) {
		Ktisis.Log.Info($"{target.Name} accepting payload from {source.Name}");

		if (target is IAttachTarget tar && source is IAttachable attach)
			this.Manager.Attach(attach, tar);
	}
}
