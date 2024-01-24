using System;

using Dalamud.Utility;

using Ktisis.Scene.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Scene.Factory.Types;

public interface IEntityBuilder<out T, out TBuilder> : IEntityBuilderBase<T, TBuilder> where T : SceneEntity where TBuilder : IEntityBuilder<T, TBuilder> {
	public T Add();
	public T Add(IComposite parent);
}

public abstract class EntityBuilder<T, TBuilder> : EntityBuilderBase<T, TBuilder> where T : SceneEntity where TBuilder : IEntityBuilder<T, TBuilder> {
	protected EntityBuilder(
		ISceneManager scene
	) : base(scene) { }
	
	protected abstract T Build();
	
	public T Add() => this.Add(this.Scene);
	
	public virtual T Add(IComposite parent) {
		if (!this.Scene.IsValid)
			throw new Exception("Attempted to build entity for invalid scene.");
		var entity = this.GetResult();
		parent.Add(entity);
		if (entity is WorldEntity worldEntity)
			worldEntity.Setup();
		return entity;
	}
	
	private T GetResult() {
		var result = this.Build();
		if (result.Name.IsNullOrEmpty())
			result.Name = result.GetType().Name;
		return result;
	}
}
