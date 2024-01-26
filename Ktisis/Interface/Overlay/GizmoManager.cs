using System;

using ImGuiNET;

using Ktisis.Data.Config;

namespace Ktisis.Interface.Overlay;

public class GizmoManager {
	private readonly Configuration _cfg;

	public GizmoManager(
		Configuration cfg
	) {
		this._cfg = cfg;
	}
	
	// Initialization
	
	private const string ImGuiVersion = "1.88";

	private bool IsInit;

	public unsafe void Initialize() {
		if (this.IsInit) return;
		
		var success = false;

		try {
			var imVer = ImGui.GetVersion();
			if (imVer != ImGuiVersion)
				throw new Exception($"ImGui version mismatch! Expected {ImGuiVersion}, got {imVer ?? "NULL"} instead.");
			
			var alloc = nint.Zero;
			var free = nint.Zero;
			var userData = (void*)null;
			ImGui.GetAllocatorFunctions(ref alloc, ref free, ref userData);
			
			var imCtx = ImGui.GetCurrentContext();
			ImGuizmo.Gizmo.Initialize(imCtx, alloc, free, (nint)userData);

			success = true;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize gizmo:\n{err}");
		}

		Ktisis.Log.Verbose($"Completed gizmo init (success: {success})");
		this.IsInit = success;
	}

	public Gizmo Create(GizmoId id) {
		if (!this.IsInit)
			throw new Exception("Can't create gizmo as ImGuizmo is not initialized.");
		return new Gizmo(this._cfg.Gizmo, id);
	}
}
