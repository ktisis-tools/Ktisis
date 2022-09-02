using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Ktisis.Structs.Actor;
using Race = Ktisis.Structs.Actor.Race;
using Tribe = Ktisis.Structs.Actor.Tribe;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		public Ktisis Plugin;
		public DataManager Data;

		public CustomizeUtil(Ktisis plugin) {
			Plugin = plugin;
			Data = plugin.DataManager;
		}

		public uint GetMakeIndex(Customize custom) {
			var r = (uint)custom.Race;
			var t = (uint)custom.Tribe;
			var g = (uint)custom.Gender;
			var i = Customize.GetRaceTribeIndex(custom.Race);
			return ((r - 1) * 4) + ((t - i) * 2) + g; // Thanks cait
		}

		public CharaMakeType? GetMakeData(Customize custom) {
			var lang = Plugin.Configuration.SheetLocale;
			var sheet = Data.GetExcelSheet<CharaMakeType>(lang);
			var index = GetMakeIndex(custom);
			return sheet == null ? null : sheet.GetRow(index);
		}
	}
}