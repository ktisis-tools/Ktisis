using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Chara;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Import;

public class CharaImportDialog : EntityEditWindow<ActorEntity> {
	private readonly IEditorContext _ctx;
	
	private readonly CharaImportUI _import;

	public CharaImportDialog(
		IEditorContext ctx,
		CharaImportUI import
	) : base(
		"Import Appearance",
		ctx,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._ctx = ctx;
		this._import = import;
		this._import.Context = ctx;
		this._import.OnNpcSelected += this.OnNpcSelected;
	}
	
	// Events

	public override void OnOpen() {
		this._import.Initialize();
	}

	public void SetMethod(LoadMethod method) {
		this._import.Method = method;
	}
	
	private void OnNpcSelected(CharaImportUI sender) => sender.ApplyTo(this.Target);
	
	// Draw UI
	
	public override void Draw() {
		this.UpdateTarget();
		
		ImGui.Text($"Importing appearance for {this.Target.Name}");
		ImGui.Spacing();
		
		this._import.DrawLoadMethods();
		ImGui.Spacing();
		this._import.DrawImport();
		ImGui.Spacing();
		this.DrawCharaApplication();
	}

	private void DrawCharaApplication() {
		this._import.DrawModesSelect();
		
		ImGui.Spacing();
		ImGui.Spacing();
		
		using var _ = ImRaii.Disabled(!this._import.HasSelection);
		if (ImGui.Button("Apply"))
			this._import.ApplyTo(this.Target);
		
		ImGui.Spacing();
	}
}
