using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Interface.Windows.ToolbarModules;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules;

namespace Ktisis.Interface.Windows;
internal record WindowButtons(DrawContentDelegate Window, FontAwesomeIcon Icon, string TooltipText);
internal delegate void DrawContentDelegate();

public class ToolbarWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private KtisisWindow? _subWindow;
	private readonly WorkspaceState _workspace;
	private IEditorInterface Interface => this._ctx.Interface;

	private List<WindowButtons> _buttons; 
	public ToolbarWindow(
		IEditorContext ctx,
		GuiManager gui
	) : base("Ktisis Toolbar") {
		this._ctx = ctx;
		this._gui = gui;
		this._workspace = new WorkspaceState(ctx);
		this.Flags = this.Flags | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
		this._buttons =  new() {
			new(this.DrawWorkspaceWindow, FontAwesomeIcon.PersonThroughWindow, "Workspace"),
			new(this.DrawObjectWindow, FontAwesomeIcon.ArrowsAlt, "Object Editor"),
			new(this.DrawActorWindow, FontAwesomeIcon.Walking, "Actor Editor"),
			new(this.DrawPosingWindow, FontAwesomeIcon.Portrait, "Pose View"),
			new(this.DrawEnvWindow, FontAwesomeIcon.CloudSun, "Environment Editor"),
			new(this.DrawCameraWindow, FontAwesomeIcon.CameraRetro, "Camera Editor"),
			new(this.DrawSceneWindow, FontAwesomeIcon.UsersLine, "Scene Editor"),
			new(this.DrawConfigWindow, FontAwesomeIcon.Cogs, "Settings"),
		};
		//this.SizeConstraints = new WindowSizeConstraints(){MaximumSize = new Vector2(-1, float.MaxValue),  MinimumSize = new Vector2(-1, 0)};
		
	}

	public override void PreDraw() {
		base.PreDraw();
		this.Size = Vector2.Zero;
		
		/*if(this._subWindow == null)
			this.Flags |= ImGuiWindowFlags.AlwaysAutoResize;
		else 
			this.Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;*/
		
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for toolbar window is stale, closing...");
		this.Close();
	}

	public override void Draw() {
		ImGuiP.CalcWindowNextAutoFitSize(ImGuiP.GetCurrentWindow());
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		// WorkspaceState
		this._workspace.Draw();
		ImGui.Spacing();
		
		// Try to center it?
		
		var offset = ((ImGuiP.GetCurrentWindow().ContentSize.X - (this._buttons.Count * (48 + spacing))-spacing) / 2);
		ImGui.SetCursorPosX(offset);
		
		// Subwindow Buttons
		foreach (var button in _buttons) {
			if (Buttons.IconButtonTooltip(button.Icon, button.TooltipText, new Vector2(48, 48)))
				button.Window();
			if(button != this._buttons.Last())
				ImGui.SameLine(0, spacing * 2);
		}
		
		//Close editor if nothing selected	
		//if (this._subWindow?.GetType() == typeof(ObjectWindow) && this._ctx.Selection.GetSelected().Count() == 0 && this._ctx.Config.Editor.CloseEditorOnDeselect)
			//this._subWindow = null;

		// Subwindow
		if (this._subWindow != null) {
			ImGui.Spacing();
			ImGui.Spacing();
			
			using var _frame = ImRaii.Group();
			this._subWindow.Draw();

		} 
	}

	internal void DrawWorkspaceWindow() => this.SetSubWindow<Workspace>();
	internal void DrawObjectWindow() => this.SetSubWindow<ObjectWindow>();
	internal void DrawActorWindow() => this.SetSubWindow<ActorWindow>();
	internal void DrawPosingWindow() => this.SetSubWindow<PosingWindow>();
	internal void DrawEnvWindow() => this.SetSubWindow<Env>();
	internal void DrawCameraWindow() => this.SetSubWindow<CameraWindow>();
	internal void DrawSceneWindow() => this.SetSubWindow<SceneWindow>();
	internal void DrawConfigWindow() => this.SetSubWindow<ConfigWindow>();
	
	
	private void SetSubWindow<T>() where T : KtisisWindow {
		if(this._subWindow?.GetType() ==  typeof(ObjectWindow) && typeof(T) != typeof(ObjectWindow))
			this._subWindow?.Close();
		if (this._subWindow?.GetType() == typeof(T)) {
			this._subWindow.OnClose();
			this._subWindow = null; // unset subwindow if same button clicked
			return;
		}
		
		if (typeof(T) == typeof(Env)) {
			var module = this._ctx.Scene.GetModule<EnvModule>();
			this._subWindow = this._gui.GetOrCreate<Env>(this._ctx.Scene, module);
		} else if (typeof(T) == typeof(ObjectWindow)) {
			this._subWindow = this.Interface.GetObjectWindow();
		} else if (typeof(T) == typeof(ConfigWindow)) {
			this._subWindow = this._gui.GetOrCreate<ConfigWindow>();
		} else if(typeof(T) == typeof(SceneWindow))
		{
			this._subWindow = this._gui.GetOrCreate<SceneWindow>(this._ctx);
		}else {
			this._subWindow = this._gui.GetOrCreate<T>(this._ctx);
		}

		ImGui.SetNextWindowSize(-1*Vector2.One);

		// handle window followup actions
		if (this._subWindow is ActorWindow win) {
			var target = this._ctx.Selection.GetFirstSelected();
			if (
				target switch {
					BoneNode node => node.Pose.Parent,
					BoneNodeGroup group => group.Pose.Parent,
					EntityPose pose => pose.Parent,
					_ => target
				} is ActorEntity actor
			) win.SetTarget(actor);
			else
				win.SetTarget(this._ctx.Scene.GetFirstActor());
		}
		this._subWindow.OnOpen();
	}
}
