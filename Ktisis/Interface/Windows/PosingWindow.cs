using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Config.Pose2D;
using Ktisis.Data.Serialization;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Posing;
using Ktisis.Interface.Components.Posing.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows.Import;
using Ktisis.Localization;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows;

public class PosingWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly LocaleManager _locale;
	private readonly PoseViewRenderer _render;

	private PoseViewSchema? Schema;
	private ViewEnum View = ViewEnum.Body;

	private enum ViewEnum {
		Body,
		Face
	}
	
	public PosingWindow(
		IEditorContext ctx,
		ITextureProvider tex,
		LocaleManager locale
	) : base(
		"Pose View"
	) {
		this._ctx = ctx;
		this._locale = locale;
		this._render = new PoseViewRenderer(ctx.Config, tex);
	}

	public override void OnOpen() {
		this.Schema = SchemaReader.ReadPoseView();
	}
	
	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for posing window is stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(500, 350)
		};
	}

	public override void Draw() {
		using var _ = ImRaii.TabBar("##pose_tabs");

		var actors = this._ctx.Scene.Children
			.Where(entity => entity is ActorEntity)
			.Cast<ActorEntity>();

		foreach (var actor in actors) {
			using var tab = ImRaii.TabItem(actor.Name);
			if (!tab.Success) continue;
			
			ImGui.Spacing();
			
			this.DrawWindow(actor);
		}
	}

	private void DrawWindow(ActorEntity target) {
		var avail = ImGui.GetContentRegionAvail();

		var width = avail.X * 0.90f;
		var spacing = ImGui.GetStyle().ItemSpacing.X * 2;
		
		var viewRegion = avail with { X = width - spacing };
		this.DrawView(target, viewRegion);
		ImGui.SameLine();
		ImGui.SetCursorPosX(width);
		this.DrawSideMenu(target);
	}
	
	// Side

	private void DrawSideMenu(ActorEntity target) {
		using var _ = ImRaii.Group();
		
		this.DrawViewSelect();
		for (var i = 0; i < 3; i++) ImGui.Spacing();
		this.DrawImportExport(target);
	}

	private void DrawViewSelect() {
		using var _ = ImRaii.Group();

		ImGui.Text("View:");
		
		foreach (var value in Enum.GetValues<ViewEnum>()) {
			if (ImGui.RadioButton(value.ToString(), this.View == value))
				this.View = value;
		}
	}

	private void DrawImportExport(ActorEntity target) {
		if (target.Pose == null) return;

		if (ImGui.Button("Import"))
			this._ctx.Interface.OpenPoseImport(target);

		if (ImGui.Button("Export"))
			this._ctx.Interface.OpenPoseExport(target.Pose);
	}
	
	// View rendering
	
	private void DrawView(ActorEntity target, Vector2 region) {
		using var _ = ImRaii.Child("##viewFrame", region, false, ImGuiWindowFlags.NoScrollbar);

		var frame = this._render.StartFrame();
		
		switch (this.View) {
			case ViewEnum.Body:
				this.DrawView(frame, "Body", 0.35f);
				ImGui.SameLine();
				this.DrawView(frame, "Armor", 0.35f);
				ImGui.SameLine();
				using (var _group = ImRaii.Group()) {
					this.DrawView(frame, "Hands", 0.30f, 0.60f);
					
					ImGui.Spacing();
					
					var hasTail = false;
					var isBunny = false;
					target.Pose?.CheckFeatures(out hasTail, out isBunny);
					
					var template = this._render.BuildTemplate(target);
					
					var width = (hasTail, isBunny) switch {
						(true, true) => 0.15f,
						(true, false) or (false, true) => 0.30f,
						_ => 0.00f
					};

					if (hasTail) {
						this.DrawView(frame, "Tail", width, 0.40f);
						if (isBunny) ImGui.SameLine();
					}
					
					if (isBunny) this.DrawView(frame, "Ears", width, 0.40f, template);
				}
				break;
			default:
				this.DrawView(frame, "Face", 0.65f);
				ImGui.SameLine();
				using (var _group = ImRaii.Group()) {
					this.DrawView(frame, "Lips", 0.35f, 0.50f);
					this.DrawView(frame, "Mouth", 0.35f, 0.50f);
				}
				break;
		}
		
		if (target.Pose != null)
			frame.DrawBones(target.Pose);
	}

	private void DrawView(
		IViewFrame frame,
		string name,
		float width = 1.0f,
		float height = 1.0f,
		IDictionary<string, string>? template = null
	) {
		if (this.Schema == null) return;

		if (!this.Schema.Views.TryGetValue(name, out var view))
			return;

		frame.DrawView(view, width, height, template);
	}
}
