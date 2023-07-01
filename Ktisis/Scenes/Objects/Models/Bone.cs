namespace Ktisis.Scenes.Objects.Models; 

public class Bone : SceneObject {
	// Bone :)
	
	public readonly string BoneName;
	
	internal uint PartialId;
	internal readonly int PartialIndex;

	public Bone(string name, int partialIndex, uint partialId) {
		Name = name;
		BoneName = name;
		PartialIndex = partialIndex;
		PartialId = partialId;
	}
}