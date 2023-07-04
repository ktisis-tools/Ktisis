using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;

using Ktisis.Core;

namespace Ktisis.Interface.Overlay;

public class GuiOverlay {
	private Gizmo? Gizmo;

	// Init

	internal void Init() {
		Gizmo = Gizmo.Create();
		if (Gizmo == null) {
			Services.PluginInterface.UiBuilder.AddNotification(
				"Failed to create gizmo. This may be due to version incompatibilities.\nPlease check your error log for more information.",
				Ktisis.VersionName, NotificationType.Warning, 10000
			);
		}
	}

	// Handle draw event

	internal void Draw() {
		// TODO: Toggle

		if (!Services.GPose.Active)
			return;

		try {
			// This passes control to the finally block.
			if (!BeginFrame()) return;
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

	// Draw frame

	private void DrawFrame() {

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
