using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Structs.Data {
	public class CharaMakeOption {
		public string Name = "";
		public byte Default;
		public MenuType Type;
		public CustomizeIndex Index;
		public byte Count;
	}

	public enum MenuType : byte {
		List = 0,
		Select = 1,
		Color = 2,
		Unknown1 = 3,
		SelectMulti = 4,
		Slider = 5
	}
}
