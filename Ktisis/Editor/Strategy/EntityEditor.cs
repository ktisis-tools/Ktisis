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
	public BaseEditor Get(SceneEntity entity);

	public T? Get<T>(SceneEntity entity) where T : BaseEditor;

	public void Update();
}

public class EntityEditor : IEntityEditor {
	private readonly IContextMediator _mediator;

	private readonly Dictionary<SceneEntity, BaseEditor> Map = new();

	public EntityEditor(
		IContextMediator mediator
	) {
		this._mediator = mediator;
	}
	
	public T? Get<T>(SceneEntity entity) where T : BaseEditor => this.Get(entity) as T;

	public BaseEditor Get(SceneEntity entity) {
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

	private BaseEditor Create(SceneEntity entity) {
		return entity switch {
			ActorEntity actor => new ActorEditor(actor),
			BoneNode bone => new BoneEditor(bone), 
			SkeletonGroup group => new GroupEditor(group),
			WorldEntity world => new ObjectEditor(world),
			_ => new BaseEditor()
		};
	}
}
