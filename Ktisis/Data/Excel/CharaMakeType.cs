using Lumina.Excel;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Structs.Extensions;

using Lumina.Excel.Sheets;

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

		public RowRef<CharaMakeCustomize>[] Features;

		public bool HasIcon => Type == MenuType.Select;
		public bool IsFeature => HasIcon && Graphics[0] == 0;
	}

	[Sheet("CharaMakeType")]
	public struct CharaMakeType(uint row) : IExcelRow<CharaMakeType> {
		// Consts

		public const int MenuCt = 28;
		public const int VoiceCt = 12;
		public const int GraphicCt = 10;
		public const int FacialFeaturesCt = 7 * 8;

		// Properties

		public uint RowId => row;

		public RowRef<Race> Race { get; set; }
		public RowRef<Tribe> Tribe { get; set; }
		public sbyte Gender { get; set; }

		public Menu[] Menus { get; set; }
		public byte[] Voices { get; set; }
		public int[] FacialFeatures { get; set; }

		public RowRef<HairMakeType> FeatureMake { get; set; }

		public RaceEnum RaceEnum => (RaceEnum)Race.RowId;
		public TribeEnum TribeEnum => (TribeEnum)Tribe.RowId;
		public Gender GenderEnum => (Gender)Gender;
		
		public static CharaMakeType Create(ExcelPage page, uint offset, uint row) {
			var features = new int[FacialFeaturesCt];
			for (var i = 0; i < FacialFeaturesCt; i++)
				features[i] = page.ReadColumn<int>(3291 + i);

			var menus = new Menu[MenuCt];
			for (var i = 0; i < MenuCt; i++) {
				var ct = page.ReadColumn<byte>(3 + 3 * MenuCt + i);
				var menu = new Menu() {
					Name = page.ReadRowRef<Lobby>(3 + i).Value.Text.ExtractText(),
					Default = page.ReadColumn<byte>(3 + 1 * MenuCt + i),
					Type = (MenuType)page.ReadColumn<byte>(3 + 2 * MenuCt + i),
					Count = ct,
					Index = (CustomizeIndex)page.ReadColumn<uint>(3 + 6 * MenuCt + i),
					Params = new uint[ct],
					Graphics = new byte[GraphicCt]
				};

				if (menu.HasIcon || menu.Type == MenuType.List) {
					for (var p = 0; p < ct; p++)
						menu.Params[p] = page.ReadColumn<uint>(3 + (7 + p) * MenuCt + i);
					for (var g = 0; g < GraphicCt; g++)
						menu.Graphics[g] = page.ReadColumn<byte>(3 + (107 + g) * MenuCt + i);
				}
				
				if (menu.IsFeature) {
					var feats = new RowRef<CharaMakeCustomize>[ct];
					for (var x = 0; x < ct; x++)
						feats[x] = new RowRef<CharaMakeCustomize>(page.Module, menu.Params[x], page.Language);
					menu.Features = feats;
				}

				menus[i] = menu;
			}
			
			return new CharaMakeType(row) {
				Race = page.ReadRowRef<Race>(0),
				Tribe = page.ReadRowRef<Tribe>(1),
				Gender = page.ReadColumn<sbyte>(2),
				FeatureMake = page.ReadRowRef<HairMakeType>((int)row),
				FacialFeatures = features,
				Menus = menus
			};
		}
	}
}
