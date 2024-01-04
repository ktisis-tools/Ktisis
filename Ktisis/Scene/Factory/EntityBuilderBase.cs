using System;

using Dalamud.Utility;

using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory;

public abstract class EntityBuilderBase<T, TBuilder> : IEntityBuilder<T, TBuilder> where T : SceneEntity where TBuilder : IEntityBuilder<T, TBuilder> {
	protected readonly ISceneManager Scene;

	protected string Name { get; set; } = string.Empty;
	
	protected EntityBuilderBase(
		ISceneManager scene
	) {
		this.Scene = scene;
	}

	protected abstract TBuilder Builder { get; }
	
	protected abstract T Build();

	public virtual TBuilder SetName(string name) {
		this.Name = name;
		return this.Builder;
	}

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
