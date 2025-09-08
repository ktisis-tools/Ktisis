using System;

using Dalamud.Bindings.ImGui;

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
			
			var alloc = (delegate*<nuint, void*, void*>)null;
			var free = (delegate*<void*, void*, void>)null;
			var userData = (void*)null;
			ImGui.GetAllocatorFunctions(&alloc, &free, &userData);
			
			var imCtx = ImGui.GetCurrentContext();
			ImGuizmo.Gizmo.Initialize((nint)imCtx.Handle, (nint)alloc, (nint)free, (nint)userData);

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
