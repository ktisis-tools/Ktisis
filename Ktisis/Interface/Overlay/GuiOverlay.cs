using System;
using System.Numerics;

using Dalamud.Logging;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Core;
using Ktisis.Services;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Overlay.Draw;

namespace Ktisis.Interface.Overlay;

public delegate void OverlayEventHandler(GuiOverlay sender);

public class GuiOverlay {
	// Dependencies

	private readonly CameraService _camera;
	private readonly GPoseService _gpose;

	private readonly SceneDraw SceneDraw;
		
	// State
	
	public bool Visible = true;

	public readonly Gizmo? Gizmo;
	
	// Constructor

	public GuiOverlay(IServiceContainer _services, CameraService _camera, GPoseService _gpose, NotifyService _notify) {
		this._camera = _camera;
		this._gpose = _gpose;
		
		this.Gizmo = Gizmo.Create();
		if (this.Gizmo == null) {
			_notify.Warning(
				"Failed to create gizmo. This may be due to version incompatibilities.\n" +
				"Please check your error log for more information."
			);
		}
		
		_services.Inject<SceneDraw>()
			.SubscribeTo(this);
	}
	
	// Events

	public OverlayEventHandler? OnOverlayDraw;
	
	// UI draw

	public void Draw() {
		// TODO: Toggle

		if (!this.Visible || !this._gpose.IsInGPose) return;

		try {
			if (BeginFrame())
				BeginGizmo();
			else return;

			try {
				this.OnOverlayDraw?.InvokeSafely(this);
			} catch (Exception err) {
				PluginLog.Error($"Error while drawing overlay:\n{err}");
			}
		} finally {
			EndFrame();
		}
	}

	private bool BeginFrame() {
		const ImGuiWindowFlags flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs;
		
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		var io = ImGui.GetIO();
		ImGui.SetNextWindowSize(io.DisplaySize);
		ImGui.SetNextWindowPos(Vector2.Zero);
		
		ImGuiHelpers.ForceNextWindowMainViewport();

		var begin = ImGui.Begin("Ktisis Overlay", flags);
		ImGui.PopStyleVar();
		return begin;
	}

	private void BeginGizmo() {
		if (this.Gizmo is null) return;

		var proj = this._camera.GetProjectionMatrix();
		var view = this._camera.GetViewMatrix();
		if (proj is Matrix4x4 projMx && view is Matrix4x4 viewMx)
			this.Gizmo.BeginFrame(viewMx, projMx);
	}

	private void EndFrame() {
		this.Gizmo?.EndFrame();
		ImGui.End();
	}
}