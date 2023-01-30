using System;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

namespace Ktisis.Services {
	public static class GPoseService {
		public static bool IsInGPose => DalamudServices.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();
		public unsafe static bool IsGposeTargetPresent() => (IntPtr)DalamudServices.Targets->GPoseTarget != IntPtr.Zero;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? DalamudServices.ObjectTable.CreateObjectReference((IntPtr)DalamudServices.Targets->GPoseTarget) : null;

		public unsafe static Actor* TargetActor => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;
	}
}