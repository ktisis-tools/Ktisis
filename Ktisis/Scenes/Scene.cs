using System;
using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Scenes.Objects;
using Ktisis.Scenes.Objects.Game;
using Ktisis.Scenes.Objects.World;

namespace Ktisis.Scenes; 

public class Scene {
	private const ushort GPoseActorIndex = 201;
	private const ushort GPoseActorCount = 42;
	
	// Objects

	public List<SceneObject> Children { get; } = new();

	// Constructor

	internal Scene() => Build();

	// Build initial state for scene

	private void Build() {
		Children.Add(new World());
		for (ushort i = 0; i < GPoseActorCount; i++)
			TryAddActor((ushort)(GPoseActorIndex + i), out _);
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