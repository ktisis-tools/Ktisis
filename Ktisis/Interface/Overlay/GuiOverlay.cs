using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Core;
using Ktisis.Scenes;
using Ktisis.Interface.SceneUi;

namespace Ktisis.Interface.Overlay; 

internal class GuiOverlay {
	// Overlay
	
	private SceneRender? SceneRender;
	
	private Gizmo? Gizmo;

	// Init

	internal void Init() {
		var scene = Ktisis.Singletons.Get<SceneManager>();
		if (scene != null)
			SceneRender = new SceneRender(this, scene);
		
		Gizmo = Gizmo.Create();
		if (Gizmo == null) {
			Ktisis.Notify(
				NotificationType.Warning,
				"Failed to create gizmo. This may be due to version incompatibilities.\n" +
				"Please check your error log for more information."
			);
		}
	}
	
	// Handle draw event

	internal void Draw() {
		// TODO: Toggle
		
		if (!Services.Game.GPose.Active)
			return;
		
		try {
			if (BeginFrame())
				BeginGizmo();
			else 
				return; // This passes control to the finally block.
			
			DrawFrame();
		} finally {
			EndFrame();
		}
	}
	
	// Begin

	private bool BeginFrame() {
		const ImGuiWindowFlags flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs;
		
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		var io = ImGui.GetIO();
		ImGui.SetNextWindowSize(io.DisplaySize);
		ImGui.SetNextWindowPos(Vector2.Zero);

		ImGuiHelpers.ForceNextWindowMainViewport();
		
		return ImGui.Begin("Ktisis Overlay", flags);
	}

	private unsafe void BeginGizmo() {
		if (Gizmo is not Gizmo gizmo) return;
		
		var camMgr = CameraManager.Instance;
		if (camMgr == null) return;

		var camera = camMgr->GetActiveCamera();
		if (camera == null) return;

		var render = camera->CameraBase.SceneCamera.RenderCamera;
		if (render == null) return;

		var proj = render->ProjectionMatrix;
		var view = camera->CameraBase.SceneCamera.ViewMatrix;
		view.M44 = 1f;

		gizmo.BeginFrame(view, proj);
	}
	
	// Draw frame

	private void DrawFrame() {
		var drawList = ImGui.GetWindowDrawList();
		try {
			SceneRender?.Draw(Gizmo);
		} catch (Exception e) {
			PluginLog.Error($"Error while drawing scene overlay:\n{e}");
		}
	}

	// End

	private void EndFrame() {
		ImGui.End();
		ImGui.PopStyleVar();
	}
	
	// Dispose

	internal void Dispose() {
		Gizmo = null;
	}
}