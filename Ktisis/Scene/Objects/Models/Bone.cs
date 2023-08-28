namespace Ktisis.Scene.Objects.Models; 

public class Bone : ArmatureNode {
	// Constructor

	public uint PartialId;
	public readonly int PartialIndex;

	public Bone(string name, uint pId, int pIndex) {
		this.Name = name;
		this.PartialId = pId;
		this.PartialIndex = pIndex;
	}
}