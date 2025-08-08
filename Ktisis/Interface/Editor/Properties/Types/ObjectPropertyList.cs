using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Editor.Properties.Types;

public abstract class ObjectPropertyList {
	public abstract void Invoke(IPropertyListBuilder builder, SceneEntity target);
}
