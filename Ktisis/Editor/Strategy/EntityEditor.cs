using System;
using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Context;
using Ktisis.Editor.Strategy.Actors;
using Ktisis.Editor.Strategy.Bones;
using Ktisis.Editor.Strategy.World;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Editor.Strategy;

public interface IEntityEditor {
	public BaseModify Get(SceneEntity entity);

	public T? Get<T>(SceneEntity entity) where T : BaseModify;

	public void Update();
}

public class EntityEditor : IEntityEditor {
	private readonly IContextMediator _mediator;

	public EntityEditor(
		IContextMediator mediator
	) {
		this._mediator = mediator;
	}
	
	private readonly Dictionary<SceneEntity, BaseModify> Map = new();
	
	public T? Get<T>(SceneEntity entity) where T : BaseModify => this.Get(entity) as T;

	public BaseModify Get(SceneEntity entity) {
		if (!entity.IsValid)
			throw new Exception("Attempting to get editor for stale entity.");
		if (this.Map.TryGetValue(entity, out var editor))
			return editor;
		editor = this.Create(entity);
		this.Map.Add(entity, editor);
		return editor;
	}

	public void Update() {
		foreach (var entity in this.Map.Keys.Where(entity => !entity.IsValid).ToList())
			this.Map.Remove(entity);
	}

	private BaseModify Create(SceneEntity entity) {
		return entity switch {
			ActorEntity actor => new ActorModify(actor),
			BoneNode bone => new BoneModify(bone), 
			SkeletonGroup group => new GroupModify(group),
			WorldEntity world => new ObjectModify(world),
			_ => new BaseModify()
		};
	}
}
