using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Editor.Characters.Make;

public class MakeTypeFeature {
	public string Name = string.Empty;
	public CustomizeIndex Index;
	public MakeTypeParam[] Params = [];
	public bool IsCustomize;
	public bool IsIcon;
}
