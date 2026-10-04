using System.Collections.Generic;

using Ktisis.Scene.Entities;

namespace Ktisis.Scene.Types;

public interface IComposite {
	public SceneEntity? Parent { get; set; }
	
	public IEnumerable<SceneEntity> Children { get; }

	public bool Add(SceneEntity entity);
	public bool Remove(SceneEntity entity);

	public IEnumerable<SceneEntity> Recurse();
}
