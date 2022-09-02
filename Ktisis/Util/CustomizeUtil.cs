using Dalamud.Data;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		public DataManager Data;

		public CustomizeUtil(Ktisis plugin) {
			Data = plugin.DataManager;
		}
	}
}