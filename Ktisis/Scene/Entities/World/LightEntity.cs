using System;

using Ktisis.Common.Utility;
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

public class LightEntity : WorldEntity, IDeletable {
	public LightEntityFlags Flags { get; set; } = LightEntityFlags.None;

	public unsafe new SceneLight* GetObject() => (SceneLight*)base.GetObject();
	
	public LightEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Light;
	}
	
	private LightModule GetModule() => this.Scene.GetModule<LightModule>();

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

	public bool Delete() {
		this.GetModule().Delete(this);
		return this.Address == nint.Zero;
	}
}
