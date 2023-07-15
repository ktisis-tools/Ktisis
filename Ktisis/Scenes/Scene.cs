using System;
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

	// Object iteration

	internal void Iterate(Action<SceneObject> callback)
		=> IterateRecurse(Children, callback);

	internal int Iterate(Func<SceneObject, int, int> callback, int initial = 0)
		=> IterateRecurse(Children, callback, initial);

	private void IterateRecurse(List<SceneObject> items, Action<SceneObject> callback) {
		foreach (var item in items) {
			callback.Invoke(item);
			IterateRecurse(item.Children, callback);
		}
	}

	private int IterateRecurse(List<SceneObject> items, Func<SceneObject, int, int> callback, int value) {
		foreach (var item in items) {
			value = callback.Invoke(item, value);
			value = IterateRecurse(item.Children, callback, value);
		}
		return value;
	}

	// Selection

	internal int UnselectAll() => Iterate((item, val) => {
		if (item.Selected) val++;
		item.Unselect();
		return val;
	});
}
