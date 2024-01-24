using System;

using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Scene.Factory.Builders;

public interface ILightBuilder : IEntityBuilder<LightEntity, ILightBuilder> {
	public ILightBuilder SetAddress(nint address);
	public unsafe ILightBuilder SetAddress(SceneLight* pointer);
}

public sealed class LightBuilder : EntityBuilder<LightEntity, ILightBuilder>, ILightBuilder {
	private nint Address = nint.Zero;

	public LightBuilder(
		ISceneManager scene
	) : base(scene) {
		this.Name = "Light";
	}
	
	protected override LightBuilder Builder => this;
	
	public ILightBuilder SetAddress(nint address) {
		this.Address = address;
		return this;
	}

	public unsafe ILightBuilder SetAddress(SceneLight* pointer) {
		this.Address = (nint)pointer;
		return this;
	}

	protected override LightEntity Build() {
		if (this.Address == nint.Zero)
			throw new Exception("Attempted to create light from null pointer.");
		
		return new LightEntity(this.Scene) {
			Name = this.Name,
			Address = this.Address
		};
	}
}
