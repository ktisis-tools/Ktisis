using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Make;

public class MakeTypeRace(Tribe tribe, Gender gender) {
	public Tribe Tribe = tribe;
	public Gender Gender = gender;

	public readonly Dictionary<CustomizeIndex, MakeTypeFeature> Customize = new();
	public readonly Dictionary<byte, uint[]> FaceFeatureIcons = new();
	
	public bool HasFeature(CustomizeIndex index) => this.Customize.ContainsKey(index);

	public MakeTypeFeature? GetFeature(CustomizeIndex index) => this.Customize.GetValueOrDefault(index);
}
