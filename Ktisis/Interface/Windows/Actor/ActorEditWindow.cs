using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Actor;

public class ActorEditWindow : KtisisWindow {
	private readonly IEditorContext _context;
	
	public ActorEntity Target { get; set; }
	
	public ActorEditWindow(
		IEditorContext context
	) : base("Actor Editor") {
		this._context = context;
	}

	public override void PreDraw() {
		if (this._context.IsValid) return;
		Ktisis.Log.Verbose("Context for actor window is stale, closing...");
		this.Close();
	}
	
	public override void Draw() {
		/*var edit = this.Target.GetEdit();

		var custom = edit.GetCustomize();
		if (custom != null) {
			ImGui.Text($"{custom.Value.Race}");
			ImGui.Text($"{custom.Value.Tribe}");

		}*/
	}
}
