using System;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Scene.Factory.Builders;

public interface IObjectBuilder : IEntityBuilder<WorldEntity, IObjectBuilder> {
	public IObjectBuilder SetAddress(nint address);
	public unsafe IObjectBuilder SetAddress(Object* pointer);
}

public sealed class ObjectBuilder : EntityBuilderBase<WorldEntity, IObjectBuilder>, IObjectBuilder {
	private readonly IPoseBuilder _pose;

	public ObjectBuilder(
		ISceneManager scene,
		IPoseBuilder pose
	) : base(scene) {
		this._pose = pose;
	}

	protected override IObjectBuilder Builder => this;
	
	private nint Address = nint.Zero;
	
	public IObjectBuilder SetAddress(nint address) {
		this.Address = address;
		return this;
	}

	public unsafe IObjectBuilder SetAddress(Object* pointer) {
		this.Address = (nint)pointer;
		return this;
	}
	
	private unsafe ObjectType GetObjectType()
		=> ((Object*)this.Address)->GetObjectType();

	private unsafe CharacterBase.ModelType GetModelType()
		=> ((CharacterBase*)this.Address)->GetModelType();

	private void SetFallbackName(string name) {
		if (this.Name.IsNullOrEmpty())
			this.Name = name;
	}

	protected override WorldEntity Build() {
		if (this.Address == nint.Zero)
			throw new Exception("Attempted to build object from null pointer.");

		var type = this.GetObjectType();
		var result = type switch {
			ObjectType.Light => new LightEntity(this.Scene),
			ObjectType.CharacterBase => this.BuildCharaBase(),
			// TODO: VFX?
			_ => this.BuildDefault()
		};
		this.SetFallbackName(type.ToString());
		result.Name = this.Name;
		result.Address = this.Address;
		return result;
	}

	private WorldEntity BuildCharaBase() {
		var type = this.GetModelType();
		this.SetFallbackName(type.ToString());
		return type switch {
			CharacterBase.ModelType.Weapon => new WeaponEntity(this.Scene, this._pose),
			// TODO: Implement generic variant of CharaEntity
			_ => this.BuildDefault()
		};
	}

	private WorldEntity BuildDefault() => new(this.Scene);
}
