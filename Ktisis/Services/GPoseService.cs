using Ktisis.Structs.Actor;

namespace Ktisis.Services {
	public static class GPoseService {
		public static bool IsInGPose => DalamudServices.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();
		public unsafe static Actor* TargetActor => (Actor*)DalamudServices.Targets->GPoseTarget;

		public unsafe static bool IsGposeTargetPresent() => (nint)TargetActor != 0;
	}
}