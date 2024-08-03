using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class ReferenceImage : SceneEntity, IVisibility, IDeletable {
	public string FilePath { get; set; } = string.Empty;

	public float Opacity = 1.0f;

	public ReferenceImage(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.RefImage;
	}
	
	public bool Visible { get; set; }
	
	public bool Delete() => throw new System.NotImplementedException();
}
