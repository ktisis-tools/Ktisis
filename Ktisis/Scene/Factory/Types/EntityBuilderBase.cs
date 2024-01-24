using Ktisis.Scene.Entities;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Types;

public interface IEntityBuilderBase<out T, out TBuilder> where T : SceneEntity where TBuilder : IEntityBuilderBase<T, TBuilder> {
	public TBuilder SetName(string name);
}

public abstract class EntityBuilderBase<T, TBuilder> : IEntityBuilderBase<T, TBuilder> where T : SceneEntity where TBuilder : IEntityBuilderBase<T, TBuilder> {
	protected readonly ISceneManager Scene;

	protected string Name { get; set; } = string.Empty;
	
	protected EntityBuilderBase(
		ISceneManager scene
	) {
		this.Scene = scene;
	}

	protected abstract TBuilder Builder { get; }

	public virtual TBuilder SetName(string name) {
		this.Name = name;
		return this.Builder;
	}
}
