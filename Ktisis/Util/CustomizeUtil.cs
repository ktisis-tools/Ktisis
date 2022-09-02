using System;
using System.Collections;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Enums;

using Lumina.Excel.GeneratedSheets;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Data;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		public Ktisis Plugin;
		public DataManager Data;

		public CharaMakeType? Cached;

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
			var index = GetMakeIndex(custom);
			if (Cached != null && Cached.RowId == index) {
				return Cached;
			} else {
				var lang = Plugin.Configuration.SheetLocale;
				var sheet = Data.GetExcelSheet<CharaMakeType>(lang);
				var row = sheet == null ? null : sheet.GetRow(index);
				Cached = row;
				return row;
			}
		}

		public CharaMakeIterator? GetIterator(Customize custom) {
			var data = GetMakeData(custom);
			return data == null ? null : new(data);
		}
	}

	public class CharaMakeIterator : IEnumerable {
		public CharaMakeType Make;

		public CharaMakeIterator(CharaMakeType make) {
			Make = make;
		}

		public CharaMakeOption GetMakeOption(int i) {
			return new CharaMakeOption() {
				Name = Make.Menu[i].Value!.Text,
				Default = Make.InitVal[i],
				Type = (MenuType)Make.SubMenuType[i],
				Index = (CustomizeIndex)Make.Customize[i],
				Count = Make.SubMenuNum[i]
			};
		}

		public CharaMakeOption this[int index] {
			get => GetMakeOption(index);
			set => new NotImplementedException();
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < 28; i++)
				yield return this[i];
		}
	}
}