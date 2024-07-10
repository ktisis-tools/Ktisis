using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace Ktisis; 

public static class GameVersion {
	public const string Validated = "2023.11.09.0000.0000";

	public unsafe static string GetCurrent() {
		var framework = Framework.Instance();
		return framework != null ? framework->GameVersionString : string.Empty;
	}
}
