using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Character;

namespace Ktisis.Interface.Editor.Properties;

public class WeaponPropertyList : ObjectPropertyList {
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not WeaponEntity weapon)
			return;
		
		//builder.AddHeader("Weapon", () => { });
	}
}
