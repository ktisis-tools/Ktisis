using System;

using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities;

public class SceneRoot(ISceneManager scene) : SceneEntity(scene) {
	public override bool IsValid => this.Scene.IsValid;
	
	public override SceneEntity? Parent {
		get => null;
		set => throw new Exception("Attempted to set parent of scene root.");
	}

	public override bool Add(SceneEntity entity) {
		Ktisis.Log.Debug($"Adding entity to scene: '{entity.Name}' ({entity.GetType().Name})");
		return base.Add(entity);
	}
}
