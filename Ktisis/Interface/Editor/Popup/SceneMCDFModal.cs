using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class SceneMCDFModal(SceneFile.ActorInfo entity, IEditorContext context)  : KtisisPopup("##PresetSave", ImGuiWindowFlags.Modal) {
	private SceneFile _sceneFile;

	protected override void OnDraw() {
		using var wrap = ImRaii.TextWrapPos(ImGui.GetWindowContentRegionMax().X);
		ImGui.TextUnformatted($"The MCDF linked to the actor {entity.Chara.Nickname} wasn't found, do you want select a file to load for them?");
		ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().Y * .80f,ImGui.GetContentRegionAvail().X * .25f));
		if (ImGui.Button("Pick File")) {
			context.Interface.OpenMcdfFile((s => {
				var f = this._sceneFile.Actors.Find(e => e.Index == entity.Index);
				f.MCDF = s;
			}));
			
		}
		if (ImGui.Button("Ignore")) {
			var f =this._sceneFile.Actors.Find(e => e.Index == entity.Index);
			f.MCDF = string.Empty;
		}
	}

	public void SetScene(ref SceneFile sceneFile) {
		this._sceneFile = sceneFile;
	}



}
