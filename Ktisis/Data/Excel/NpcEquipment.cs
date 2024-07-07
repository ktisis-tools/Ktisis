using Lumina;
using Lumina.Data;
using Lumina.Excel;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
    [Sheet("NpcEquip", columnHash: 0xe91c87ba)]
	public class NpcEquipment : ExcelRow {
		public WeaponEquip MainHand { get; private set; }
		public WeaponEquip OffHand { get; private set; }

		public Equipment Equipment { get; private set; }

		public override void PopulateData(RowParser parser, GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			this.MainHand = parser.ReadWeapon(0);
			this.OffHand = parser.ReadWeapon(3);
			this.Equipment = parser.ReadEquipment(6);
		}
	}
}
