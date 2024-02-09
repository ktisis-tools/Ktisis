using System.Collections.Generic;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;

namespace Ktisis.Editor.Transforms.Types;

public interface ITransformTarget : ITransform {
	public SceneEntity? Primary { get; }
	public IEnumerable<SceneEntity> Targets { get; }

	public TransformSetup Setup { get; set; }
}
