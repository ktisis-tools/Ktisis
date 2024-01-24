using System.Threading.Tasks;

using Ktisis.Scene.Entities;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Types;

public interface IEntityCreator<T, out TBuilder> : IEntityBuilderBase<T, TBuilder> where T : SceneEntity where TBuilder : IEntityBuilderBase<T, TBuilder> {
	public Task<T> Spawn();
}

public abstract class EntityCreator<T, TBuilder> : EntityBuilderBase<T, TBuilder> where T : SceneEntity where TBuilder : IEntityCreator<T, TBuilder> {
	protected EntityCreator(
		ISceneManager scene
	) : base(scene) { }
}
