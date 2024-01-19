using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Interface.Components.Actors.Data;

public class MakeTypeFeature {
	public string Name = string.Empty;
	public CustomizeIndex Index;
	public MakeTypeParam[] Params = [];
	public bool IsCustomize;
	public bool IsIcon;
}
