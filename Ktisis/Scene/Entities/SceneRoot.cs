using System;

using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities;

public class SceneRoot(ISceneManager scene) : SceneEntity(scene) {
	public override bool IsValid => this.Scene.IsValid;
	
	public override SceneEntity? Parent {
		get => null;
		set => throw new Exception("Attempted to set parent of scene root.");
	}

	public void SetChildsIndex(SceneEntity child, int index) {
		if (!child.IsChildOf(this)) return;
		this.GetChildren().Remove(child);
		this.GetChildren().Insert(index, child);
	}
	public override bool Add(SceneEntity entity) {
		if (entity is ActorEntity actor)
			Ktisis.Log.Debug($"Adding actor to scene: '{actor.Name}' (index: {actor.Actor.ObjectIndex})");
		else
			Ktisis.Log.Debug($"Adding entity to scene: '{entity.Name}' ({entity.GetType().Name})");
		return base.Add(entity);
	}
}
