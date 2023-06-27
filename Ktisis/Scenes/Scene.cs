using System;
using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Scenes.Objects;
using Ktisis.Scenes.Objects.Game;

namespace Ktisis.Scenes; 

public class Scene {
	private const ushort GPoseActorIndex = 201;
	
	// Objects

	public List<SceneObject> Children { get; } = new();

	// Constructor

	internal Scene() => Build();

	// Build initial state for scene

	private void Build() {
		TryAddActor(GPoseActorIndex, out _);
	}
	
	// Update state

	internal void Update()
		=> Children.ForEach(UpdateItem);

	private void UpdateItem(SceneObject item) {
		try {
			item.Update();
		} catch (Exception e) {
			PluginLog.Error($"Error while updating object state:\n{e}");
		}
	}
	
	// Actors

	public bool TryAddActor(ushort id, out Actor? actor, bool addToScene = true) {
		actor = Actor.FromIndex(id);
		var isValid = actor != null;
		if (isValid && addToScene) Children.Add(actor!);
		return isValid;
	}
}