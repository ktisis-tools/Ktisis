using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;


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
internal record WindowButtons(DrawContentDelegate Window, FontAwesomeIcon Icon, string TooltipText, Type WindowType);
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
			new(this.DrawWorkspaceWindow, FontAwesomeIcon.PersonThroughWindow, "Workspace", typeof(Workspace)),
			new(this.DrawObjectWindow, FontAwesomeIcon.ArrowsAlt, "Object Editor", typeof(ObjectWindow)),
			new(this.DrawActorWindow, FontAwesomeIcon.Walking, "Actor Editor", typeof(ActorWindow)),
			new(this.DrawPosingWindow, FontAwesomeIcon.Portrait, "Pose View", typeof(PosingWindow)),
			new(this.DrawEnvWindow, FontAwesomeIcon.CloudSun, "Environment Editor", typeof(Env)),
			new(this.DrawCameraWindow, FontAwesomeIcon.CameraRetro, "Camera Editor", typeof(CameraWindow)),
			//new(this.DrawConfigWindow, FontAwesomeIcon.G, "Scene Editor"),
			new(this.DrawConfigWindow, FontAwesomeIcon.Cogs, "Settings", typeof(ConfigWindow)),
		};
	}

	public override void PreDraw() {
		base.PreDraw();
		this.Size = Vector2.Zero;
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for toolbar window is stale, closing...");
		this.Close();
	}

	public override void Draw() {
		using var a = ImRaii.PushId("##ToolbarMain");
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// WorkspaceState
		this._workspace.Draw();
		ImGui.Spacing();
		
		// Try to center it?
		
		var offset = ((ImGuiP.GetCurrentWindow().ContentSize.X - (this._buttons.Count * (48 + spacing))-spacing) / 2);
		ImGui.SetCursorPosX(offset);
		
		// Subwindow Buttons
		foreach (var button in _buttons) {
			Vector4 color;
			ImGuiCol bgCol;
			if(button.WindowType != typeof(PosingWindow))
				bgCol= this._subWindow?.GetType() == button.WindowType ? ImGuiCol.ButtonActive : ImGuiCol.Button;
			else {
				bgCol = this._ctx.Plugin.Gui.Get<PosingWindow>() is { IsOpen: true } ? ImGuiCol.ButtonActive : ImGuiCol.Button;
			}
			unsafe {
				color =  *ImGui.GetStyleColorVec4(bgCol);
			}
			using var _ = ImRaii.PushColor(ImGuiCol.Button, color);
			if (Buttons.IconButtonTooltip(button.Icon, button.TooltipText, new Vector2(48, 48)))
				button.Window();
			if(button != this._buttons.Last())
				ImGui.SameLine(0, spacing * 2);
		}
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
	internal void DrawPosingWindow() => this.Interface.OpenPosingWindow();
	internal void DrawEnvWindow() => this.SetSubWindow<Env>();
	internal void DrawCameraWindow() => this.SetSubWindow<CameraWindow>();
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
		} else if(typeof(T) == typeof(ActorWindow))
		{
			this._subWindow = this._gui.GetOrCreate<T>(this._ctx);
			this._subWindow.Size = new Vector2(0, 400);
		}else {
			this._subWindow = this._gui.GetOrCreate<T>(this._ctx);
		}

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

	public override void OnClose() {
		this._subWindow?.Close();
		this._subWindow?.OnSafeToRemove();
		base.OnClose();
	}
}
