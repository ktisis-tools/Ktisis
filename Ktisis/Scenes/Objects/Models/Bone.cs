using Dalamud.Logging;

using Ktisis.Common.Utility;

namespace Ktisis.Scenes.Objects.Models; 

public class Bone : SceneObject {
	private readonly Armature Owner;
	
	public readonly string BoneName;
	
	internal uint PartialId;
	internal readonly int PartialIndex;

	// Constructor
	
	public Bone(Armature armature, string name, int partialIndex, uint partialId) {
		Owner = armature;
		
		Name = name;
		BoneName = name;
		
		PartialId = partialId;
		PartialIndex = partialIndex;
	}

	// Object
	
	public Transform GetTransform() => throw new System.NotImplementedException();
	public void SetTransform(Transform trans) => throw new System.NotImplementedException();
	
	public void Draw() {
		PluginLog.Information("im draw bone :)");
	}
}