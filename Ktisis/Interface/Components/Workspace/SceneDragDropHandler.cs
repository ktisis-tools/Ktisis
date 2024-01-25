using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Components.Workspace;

public class SceneDragDropHandler {
	private readonly IEditorContext _context;

	private Configuration Config => this._context.Config;
	
	public SceneDragDropHandler(
		IEditorContext context
	) {
		this._context = context;
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
		using var _src = ImRaii.DragDropSource(ImGuiDragDropFlags.SourceNoDisableHover);
		if (!_src.Success) return;
		
		ImGui.SetDragDropPayload(PayloadId, nint.Zero, 0);

		this.Source = entity;

		var display = this.Config.Editor.GetDisplayForType(entity.Type);
		using var _color = ImRaii.PushColor(ImGuiCol.Text, display.Color);

		var icon = display.Icon;
		if (icon != FontAwesomeIcon.None) {
			Icons.DrawIcon(icon);
			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		}
		ImGui.Text(entity.Name);
	}

	private unsafe void HandleTarget(SceneEntity entity) {
		using var _tar = ImRaii.DragDropTarget();
		if (!_tar.Success) return;

		var pl = ImGui.AcceptDragDropPayload(PayloadId);		
		if (pl.NativePtr != null && this.Source is SceneEntity source)
			this.HandlePayload(entity, source);
	}

	private unsafe void HandlePayload(SceneEntity target, SceneEntity source) {
		Ktisis.Log.Info($"{target.Name} accepting payload from {source.Name}");

		if (target is IAttachTarget tar && source is IAttachable attach) {
			tar.AcceptAttach(attach);
			var p = attach.GetAttach();
			Ktisis.Log.Info($"{p != null}");
		}
	}
}
