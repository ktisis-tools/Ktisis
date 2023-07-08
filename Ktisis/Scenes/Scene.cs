using System.Collections.Generic;

using Ktisis.Scenes.Objects;
using Ktisis.Scenes.Managers;

namespace Ktisis.Scenes; 

public class Scene : SceneObject {
	// Managers

	public readonly ActorManager ActorManager;
	public readonly LightManager LightManager;

	// Objects

	public override List<SceneObject> Children { get; init; } = new();

	// Constructor

	internal Scene() {
		ActorManager = new ActorManager(this);
		LightManager = new LightManager(this);
		
		Build();
	}

	// Build initial state for scene

	private void Build() {
		ActorManager.AddGPoseActors();
	}
	
	// Update scene state

	internal override void Update() {
		LightManager.Update();
		
		base.Update();
	}
}