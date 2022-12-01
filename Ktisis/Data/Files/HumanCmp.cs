using Ktisis.Structs.Actor;

namespace Ktisis.Data.Files {
	public class HumanCmp {
		public uint[] Colors;

		public HumanCmp() {
			var file = Services.DataManager.GetFile("chara/xls/charamake/human.cmp")!;

			Colors = new uint[file.Data.Length >> 2];
			for (var i = 0; i < file.Data.Length; i += 4) {
				uint col = 0;
				for (var x = 0; x < 4; x++)
					col |= (uint)file.Data[i + x] << (x * 8);
				Colors[i >> 2] = col;
			}
		}

		public uint[] GetEyeColors()
			=> Colors[0..192];

		public uint[] GetHairHighlights()
			=> Colors[256..448];

		public uint[] GetFacepaintColors() {
			var colors = new uint[224];
			Colors[512..608].CopyTo(colors, 0);
			Colors[640..736].CopyTo(colors, 128);
			return colors;
		}
		
		public uint[] GetLipColors() {
			var colors = new uint[224];
			Colors[512..608].CopyTo(colors, 0);
			Colors[1792..1888].CopyTo(colors, 128);
			return colors;
		}

		public uint[] GetSkinColors(Tribe tribe, Gender gender) {
			var start = GetTribeSkinIndex(tribe, gender);
			return Colors[start..(start+192)];
		}

		public uint[] GetHairColors(Tribe tribe, Gender gender) {
			var start = GetTribeHairIndex(tribe, gender);
			return Colors[start..(start+192)];
		}

		public static int GetTribeSkinIndex(Tribe tribe, Gender gender) {
			var genderOffset = gender == Gender.Masculine ? 0 : 1;
			return (((int)tribe * 2 + genderOffset) * 5 + 3) * 256;
		}

		public static int GetTribeHairIndex(Tribe tribe, Gender gender) {
			var genderOffset = gender == Gender.Masculine ? 0 : 1;
			return (((int)tribe * 2 + genderOffset) * 5 + 4) * 256;
		}
	}
}