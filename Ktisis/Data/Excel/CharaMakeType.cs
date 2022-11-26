using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Dalamud.Game.ClientState.Objects.Enums;

using RaceEnum = Ktisis.Structs.Actor.Race;
using TribeEnum = Ktisis.Structs.Actor.Tribe;
using Gender = Ktisis.Structs.Actor.Gender;

namespace Ktisis.Data.Excel {
	public enum MenuType : byte {
		List = 0,
		Select = 1,
		Color = 2,
		Unknown1 = 3,
		SelectMulti = 4,
		Slider = 5
	}

	public struct Menu {
		public string Name;
		public byte Default;
		public MenuType Type;
		public byte Count;
		// LookAt
		// SubMenuMask
		public CustomizeIndex Index;
		public uint[] Params;
		public byte[] Graphics;

		public LazyRow<CharaMakeCustomize>[] Features;

		public bool HasIcon => Type == MenuType.Select;
		public bool IsFeature => HasIcon && Graphics[0] == 0;
	}

	[Sheet("CharaMakeType")]
	public class CharaMakeType : ExcelRow {
		// Consts

		public const int MenuCt = 28;
		public const int VoiceCt = 12;
		public const int GraphicCt = 10;

		// Properties

		public LazyRow<Race> Race { get; set; } = null!;
		public LazyRow<Tribe> Tribe { get; set; } = null!;
		public sbyte Gender { get; set; }

		public Menu[] Menus { get; set; } = new Menu[MenuCt];
		public byte[] Voices { get; set; } = new byte[VoiceCt];
		public int[] FacialFeatures { get; set; } = new int[7 * 8];

		public LazyRow<HairMakeType> FeatureMake { get; set; } = null!;

		public RaceEnum RaceEnum => (RaceEnum)Race.Row;
		public TribeEnum TribeEnum => (TribeEnum)Tribe.Row;
		public Gender GenderEnum => (Gender)Gender;

		// Build sheet

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			Race = new LazyRow<Race>(gameData, parser.ReadColumn<int>(0), language);
			Tribe = new LazyRow<Tribe>(gameData, parser.ReadColumn<int>(1), language);
			Gender = parser.ReadColumn<sbyte>(2);

			FeatureMake = new LazyRow<HairMakeType>(gameData, RowId, language);

			for (var i = 0; i < 7 * 8; i++)
				FacialFeatures[i] = parser.ReadColumn<int>(3291 + i);

			for (var i = 0; i < MenuCt; i++) {
				var ct = parser.ReadColumn<byte>(3 + 3 * MenuCt + i);
				var menu = new Menu() {
					Name = new LazyRow<Lobby>(gameData, parser.ReadColumn<uint>(3 + i), language).Value!.Text,
					Default = parser.ReadColumn<byte>(3 + 1 * MenuCt + i),
					Type = (MenuType)parser.ReadColumn<byte>(3 + 2 * MenuCt + i),
					Count = ct,
					Index = (CustomizeIndex)parser.ReadColumn<uint>(3 + 6 * MenuCt + i),
					Params = new uint[ct],
					Graphics = new byte[GraphicCt]
				};

				if (menu.HasIcon || menu.Type == MenuType.List) {
					for (var p = 0; p < ct; p++)
						menu.Params[p] = parser.ReadColumn<uint>(3 + (7 + p) * MenuCt + i);
					for (var g = 0; g < GraphicCt; g++)
						menu.Graphics[g] = parser.ReadColumn<byte>(3 + (107 + g) * MenuCt + i);
				}

				if (menu.IsFeature) {
					var feats = new LazyRow<CharaMakeCustomize>[ct];
					for (var x = 0; x < ct; x++)
						feats[x] = new LazyRow<CharaMakeCustomize>(gameData, menu.Params[x]);
					menu.Features = feats;
				}

				Menus[i] = menu;
			}
		}
	}
}