using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;


using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Types;

using Lumina.Extensions;

namespace Ktisis.Interface.Components.Workspace;

public class SceneDragDropHandler {
	private readonly IEditorContext _ctx;

	private bool UnhandledType(EntityType type) => type is not (EntityType.BoneGroup or EntityType.Armature or EntityType.BoneNode or EntityType.Invalid or EntityType.ModelSlot or EntityType.Weapon);
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
		if (!UnhandledType(entity.Type)) return;
		using var src = ImRaii.DragDropSource(ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.SourceNoDisableHover | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
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

		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var itemSize = ImGui.GetItemRectSize();

		bool cursorIsAbove = ImGui.GetMousePos().Y < (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.75f));

		bool cursorIsBelow = ImGui.GetMousePos().Y >= (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.25f));


			if (cursorIsAbove) {
				ImGui.GetWindowDrawList().AddLine(
					new Vector2(itemMin.X, itemMin.Y),
					new Vector2(itemMax.X, itemMin.Y),
					ImGui.GetColorU32(ImGuiCol.DragDropTarget)
				);
			} else if (cursorIsBelow) {
				ImGui.GetWindowDrawList().AddLine(
					new Vector2(itemMin.X, itemMax.Y),
					new Vector2(itemMax.X, itemMax.Y),
					ImGui.GetColorU32(ImGuiCol.DragDropTarget)
				);
			} else {
				ImGui.GetWindowDrawList().AddRect(itemMin, itemMax, ImGui.GetColorU32(ImGuiCol.DragDropTarget));
			}
		var pl = ImGui.AcceptDragDropPayload(PayloadId);
		if (pl.Handle != null && this._ctx.Selection.Count > 1 ) {
			foreach (var src in this._ctx.Selection.GetSelected().OrderBy(p => this._ctx.Scene.Children.Index().First(c => c.Item == p))) {
				this.HandlePayload(entity, src);
			}

		}else if (pl.Handle != null && this.Source is SceneEntity source)
			this.HandlePayload(entity, source);
	}
	
	// https://github.com/grittyfrog/MacroMate/blob/4b41fc0b40c6156fe67b0a3f64d83a4f00e430ae/MacroMate/Windows/MainWindow.cs#L529-L590

	private unsafe void HandlePayload(SceneEntity target, SceneEntity source) {
		var payload = ImGui.GetDragDropPayload();
		var itemSize = ImGui.GetItemRectSize();

		// For "beside" drag and drop (i.e. sibling drop) we need to decide how much of the item will count as "above" and "below"
		//
		// If we're also allowing an "into" drop we need to leave some room for the "into" part. This gives us the following spacing:
		//
		//                             | "Above" Size | "Into" Size | "Below" Size |
		//     ------------------------+--------------+-------------+--------------+
		//     allowInto + allowBeside | 25%          | 50%         | 25%          |
		//     allowInto               | 0%           | 100%        | 0%           |
		//                 allowBeside | 50%          | 0%          | 50%          |
		//

		bool cursorIsAbove = ImGui.GetMousePos().Y < (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.75f));

		bool cursorIsBelow = ImGui.GetMousePos().Y >= (ImGui.GetItemRectMax().Y - (itemSize.Y * 0.25f));

		if (payload.IsDelivery()) {

			if (target.IsChildOf(source))
				return; //are you fucking stupid?
			Ktisis.Log.Info($"{target.Name} accepting payload from {source.Name}");
			lock (this._ctx.Scene.Children) {
				if (cursorIsBelow || cursorIsAbove) {

					int index = target.Parent!.Children.Index().First(c => c.Item == target).Index;

					if (cursorIsBelow)
						index += 1;
					
					if (source.Type == EntityType.Folder && target.Parent != source.Parent) { //Assume this is a reorder if it happens
						var id = source.Root.Children.Index().FirstOrNull(c => target.IsChildOf(c.Item));
						if (id.HasValue)
							index = id.Value.Index;
						else
							index = source.Root.Children.Count();
						
						if (cursorIsBelow)
							index += 1;
						var parent = source.Parent;
						source.Parent!.Remove(source);		
						parent!.AddAtIndex(source, index);
						source.Parent = parent;
						source.Parent.Update();
						return;
					}

					source.Parent?.Remove(source);
					target.Parent!.AddAtIndex(source, index);
					source.Parent = target.Parent;
					target.Parent.Update();
				} else {
					if (target is IAttachTarget tar && source is IAttachable attach)
						this.Manager.Attach(attach, tar);
					if (target is FolderEntity && UnhandledType(source.Type)) {
						target.Add(source);
						source.Parent = target;
						target.Update();
					} else if (source.Parent?.Type == EntityType.Folder) {
						source.Parent.Remove(source);
						this._ctx.Scene.Add(source);
						this._ctx.Scene.Refresh();
					}
				}

			}
		}
	}
}
