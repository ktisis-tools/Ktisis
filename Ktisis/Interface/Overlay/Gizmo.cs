using System;

using ImGuiNET;

using Dalamud.Logging;

namespace Ktisis.Interface.Overlay; 

internal class Gizmo {
	// Static
	
	private const string ImGuiVersion = "1.88";

	private static bool IsInit;
	private unsafe static bool InitLibrary() {
		var success = false;
		
		try {
			var imVer = ImGui.GetVersion();
			if (imVer != ImGuiVersion)
				throw new Exception($"ImGui version mismatch! Expected {ImGuiVersion}, got {imVer ?? "NULL"} instead.");
			
			var ctx = ImGui.GetCurrentContext();

			var alloc = nint.Zero;
			var free = nint.Zero;
			var userData = (void*)null;
			ImGui.GetAllocatorFunctions(ref alloc, ref free, ref userData);

			ImGuizmo.Gizmo.Initialize(ctx, alloc, free, (nint)userData);

			success = true;
		} catch (Exception e) {
			PluginLog.Error($"Failed to initialize gizmo:\n{e}");
		}
		
		PluginLog.Verbose($"Completed gizmo initialization (success: {success}).");

		return IsInit = success;
	}

	internal static Gizmo? Create() {
		if (!Gizmo.IsInit && !Gizmo.InitLibrary())
			return null;
		return new Gizmo();
	}
}