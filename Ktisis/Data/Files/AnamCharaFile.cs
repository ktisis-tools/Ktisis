using System.Collections.Generic;

namespace Ktisis.Data.Files {
	public class AnamCharaFile {
		private static Dictionary<string, string> EnumConversions = new() {
			{ "LegacyTattoo", "Legacy" },
			{ "Lalafel", "Lalafell" },
			{ "SeekerOfTheSun", "SunSeeker" },
			{ "KeeperOfTheMoon", "MoonKeeper" },
			{ "Helions", "Helion" },
			{ "TheLost", "Lost" }
		};
	}
}