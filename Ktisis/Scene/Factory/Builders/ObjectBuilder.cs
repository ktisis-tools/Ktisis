using System;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;
using Ktisis.Services;
using Ktisis.Services.Data;

namespace Ktisis.Scene.Factory.Builders;

public interface IObjectBuilder : IEntityBuilder<WorldEntity, IObjectBuilder> {
	public IObjectBuilder SetAddress(nint address);
	public unsafe IObjectBuilder SetAddress(Object* pointer);
}

public sealed class ObjectBuilder : EntityBuilder<WorldEntity, IObjectBuilder>, IObjectBuilder {
	private readonly IPoseBuilder _pose;
	private readonly INameResolver _naming;

	public ObjectBuilder(
		ISceneManager scene,
		IPoseBuilder pose,
		INameResolver naming
	) : base(scene) {
		this._pose = pose;
		this._naming = naming;
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
		var result = type switch {
			CharacterBase.ModelType.Weapon => this.BuildWeapon(),
			// TODO: Implement generic variant of CharaEntity
			_ => new CharaEntity(this.Scene, this._pose)
			//_ => this.BuildDefault()
		};
		this.SetFallbackName(type.ToString());
		return result;
	}
	
	// Weapons

	private WeaponEntity BuildWeapon() {
		var entity = new WeaponEntity(this.Scene, this._pose);
		if (this.Name.IsNullOrEmpty() && this.GetWeaponName() is string name)
			this.Name = name;
		return entity;
	}

	private unsafe string? GetWeaponName() {
		var weapon = (Weapon*)this.Address;
		return this._naming.GetWeaponName(weapon->ModelSetId, weapon->SecondaryId, weapon->Variant);
	}
	
	// Other

	private WorldEntity BuildDefault() => new(this.Scene);
}
