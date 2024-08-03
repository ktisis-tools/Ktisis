using Ktisis.Data.Config;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class ReferenceImage : SceneEntity, IVisibility, IDeletable {
	public record SetupData {
		public string Id = string.Empty;
		
		public string FilePath = string.Empty;
		public float Opacity = 1.0f;
		
		public bool Visible = true;
	}

	public readonly SetupData Data;

	private Configuration Config => this.Scene.Context.Config;

	public ReferenceImage(
		ISceneManager scene,
		SetupData data
	) : base(scene) {
		this.Data = data;
		this.Type = EntityType.RefImage;
	}

	public bool Visible {
		get => this.Data.Visible;
		set => this.Data.Visible = value;
	}

	public void Save() {
		var list = this.Config.Editor.ReferenceImages;
		this.Data.Id = $"{list.Count}-{this.Data.GetHashCode():X}";
		list.Add(this.Data);
	}

	public bool Delete() {
		this.Config.Editor.ReferenceImages.Remove(this.Data);
		this.Remove();
		return true;
	}
}
