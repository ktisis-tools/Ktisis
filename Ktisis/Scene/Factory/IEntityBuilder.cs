using Ktisis.Scene.Entities;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory;

public interface IEntityBuilder<out T, out TBuilder> where T : SceneEntity where TBuilder : IEntityBuilder<T, TBuilder> {
	public TBuilder SetName(string name);
	
	public T Add();
	public T Add(IComposite parent);
}
