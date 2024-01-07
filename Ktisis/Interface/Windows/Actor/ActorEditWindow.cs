using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Actors;
using Ktisis.Interface.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Actor;

public class ActorEditWindow : KtisisWindow {
	private readonly IEditorContext _context;
	private readonly EquipmentEditor _equip;

	public ActorEntity Target { get; set; } = null!;
	
	public ActorEditWindow(
		IEditorContext context,
		EquipmentEditor equip
	) : base("Actor Editor") {
		this._context = context;
		this._equip = equip;
	}

	public override void PreDraw() {
		if (this._context.IsValid) return;
		Ktisis.Log.Verbose("Context for actor window is stale, closing...");
		this.Close();
	}
	
	public override unsafe void Draw() {
		var modify = this.Target as ICharacter;
		if (modify == null) return;

		var chara = modify.GetCharacter();
		if (chara == null) return;

		var equip = modify.GetEquipment();
		if (equip == null) return;

		this._equip.Draw(chara, equip);
	}
}
