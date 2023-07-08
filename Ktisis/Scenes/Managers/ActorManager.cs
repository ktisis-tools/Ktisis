using Ktisis.Scenes.Objects.Game;

namespace Ktisis.Scenes.Managers; 

public class ActorManager : ManagerBase {
	private const ushort GPoseActorIndex = 201;
	private const ushort GPoseActorCount = 42;
	
	// Constructor
	
	public ActorManager(Scene scene) : base(scene) { }
	
	// Actors

	public void AddGPoseActors()
		=> AddRange(GPoseActorIndex, GPoseActorCount);

	public void AddRange(ushort start, ushort ct) {
		for (var i = start; i < start + ct; i++)
			GetExisting(i);
	}
	
	public Actor? GetExisting(ushort id, bool addToScene = true) {
		var actor = Actor.FromIndex(id);

		if (actor != null && addToScene)
			Scene.AddChild(actor);
		
		return actor;
	}
}