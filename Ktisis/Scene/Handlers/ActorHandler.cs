using Dalamud.Plugin.Services;

using Ktisis.Scene.Objects.Game;

namespace Ktisis.Scene.Handlers; 

public class ActorHandler {
	// Constructor
	
	private readonly IObjectTable _actors;

	private readonly SceneGraph Scene;

	public ActorHandler(SceneGraph scene, IObjectTable _actors) {
		this._actors = _actors;
		
		this.Scene = scene;
		scene.OnSceneBuild += OnSceneBuild;
	}
	
	// Event handlers

	private void OnSceneBuild(SceneGraph _)
		=> this.AddGPoseActors();
	
	// Actors
	
	private const ushort GPoseActorIndex = 201;
	private const ushort GPoseActorCount = 42;

	public void AddGPoseActors()
		=> AddRange(GPoseActorIndex, GPoseActorCount);

	public void AddRange(ushort start, ushort ct) {
		for (var i = start; i < start + ct; i++)
			GetExisting(i);
	}

	public Actor? GetExisting(ushort id, bool addToScene = true) {
		var gameObj = this._actors[id];
		if (gameObj is null) return null;

		var actor = new Actor(gameObj);
		if (addToScene)
			this.Scene.AddChild(actor);
		return actor;
	}
}