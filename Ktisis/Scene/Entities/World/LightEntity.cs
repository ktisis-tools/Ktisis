using System;

using Ktisis.Common.Utility;
using Ktisis.Data.Config.Gobos;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Scene.Entities.World;

[Flags]
public enum LightEntityFlags {
	None = 0,
	Update = 1
}

public class LightEntity : WorldEntity, IDeletable, IHideable {
	public LightEntityFlags Flags { get; set; } = LightEntityFlags.None;
	public GoboEntry? Gobo { get; set; }

	public unsafe bool IsHidden {
		get {
			var ptr = this.GetObject();
			return ptr != null && !ptr->DrawObject.IsVisible;
		}
		set {
			var ptr = this.GetObject();
			if (ptr != null)
				ptr->DrawObject.IsVisible = !ptr->DrawObject.IsVisible;
		}
	}

	public unsafe new SceneLight* GetObject() => (SceneLight*)base.GetObject();
	
	public LightEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Light;
	}
	
	private LightModule GetModule() => this.Scene.GetModule<LightModule>();

	public unsafe void SetType(LightType type) {
		var ptr = this.GetObject();
		if (ptr == null || ptr->RenderLight == null) return;
		ptr->RenderLight->LightType = type;
	}

	public override void Update() {
		if (!this.IsValid) return;
		
		if (this.Flags.HasFlag(LightEntityFlags.Update))
			this.GetModule().UpdateLightObject(this);
		
		base.Update();
	}

	public override void SetTransform(Transform trans) {
		base.SetTransform(trans);
		this.Flags |= LightEntityFlags.Update;
	}

	public void ToggleHidden() => this.IsHidden = !this.IsHidden;

	public unsafe void RemoveGobo() {
		this.Gobo = null;
		var sceneLight = this.GetObject();
		if (sceneLight != null && sceneLight->Texture != null) {
			sceneLight->Texture->DecRef();
			sceneLight->Texture = null;
		}
		if (sceneLight != null && sceneLight->RenderLight != null && sceneLight->RenderLight->Texture != null)
			sceneLight->RenderLight->Texture = null;
	}
	public unsafe void SetGobo(GoboEntry selected) {
		this.Gobo = selected;
		this.Scene.GetModule<LightModule>().UpdateSceneLightTexture(this.GetObject(), selected.Path);
	}

	public bool Delete() {
		this.GetModule().Delete(this);
		return this.Address == nint.Zero;
	}
}
