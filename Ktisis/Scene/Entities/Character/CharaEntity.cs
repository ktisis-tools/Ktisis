using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Character;

public abstract class CharaEntity : WorldEntity {
	private readonly IPoseBuilder _pose;

	protected CharaEntity(
		ISceneManager scene,
		IPoseBuilder pose
	) : base(scene) {
		this._pose = pose;
	}

	public virtual unsafe CharacterBase* GetCharacter() => (CharacterBase*)this.GetObject();

	public override void Setup() {
		base.Setup();
		this._pose.Add(this);
	}

	public override void Update() {
		if (this.IsDrawing())
			base.Update();
	}

	public unsafe bool IsDrawing() {
		var ptr = this.GetCharacter();
		if (ptr == null) return false;
		return (ptr->UnkFlags_01 & 2) != 0 && ptr->UnkFlags_02 != 0;
	}
}
