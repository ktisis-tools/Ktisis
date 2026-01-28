using System;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

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
			
			delegate*<nuint, void*, void*>  allocTmp = null;
			delegate*<void*, void*, void> freeTmp =null;
			void* userData = null;
			ImGui.GetAllocatorFunctions(&allocTmp, &freeTmp, &userData);
			
			var imCtx = ImGui.GetCurrentContext();

			delegate* unmanaged[Cdecl]<nuint, void*, void*> alloc =
				(delegate* unmanaged[Cdecl]<nuint, void*, void*>)allocTmp;

			delegate* unmanaged[Cdecl]<void*, void*, void> free =
				(delegate* unmanaged[Cdecl]<void*, void*, void>)freeTmp;

			ImGuizmo.SetImGuiContext(imCtx.Handle);
			var allocDel = Marshal.GetDelegateForFunctionPointer<ImGuiMemAllocFunc>((nint)alloc);
			var freeDel  = Marshal.GetDelegateForFunctionPointer<ImGuiMemFreeFunc>((nint)free);

			ImGui.SetAllocatorFunctions(allocDel, freeDel, userData);
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
