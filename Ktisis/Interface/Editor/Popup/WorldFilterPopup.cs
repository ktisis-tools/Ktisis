using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Editor.Popup;

public class WorldFilterPopup(IEditorContext ctx) : KtisisPopup("##WorldFilterPopup") {
	protected override void OnDraw() {
		var objects = ctx.Config.Overlay.ActiveWorldFilters.HasFlag(WorldFilters.Objects);
		var actors = ctx.Config.Overlay.ActiveWorldFilters.HasFlag(WorldFilters.Actors);
		var lights = ctx.Config.Overlay.ActiveWorldFilters.HasFlag(WorldFilters.Lights);

		using (ImRaii.PushColor(ImGuiCol.Text, ctx.Config.Overlay.WorldNodeColor))
			if (ImGui.Checkbox(Ktisis.Locale.Translate("popups.world_filters.objects"), ref objects))
				ctx.Config.Overlay.ActiveWorldFilters ^= WorldFilters.Objects;
		using (ImRaii.PushColor(ImGuiCol.Text, ctx.Config.Overlay.ActorNodeColor))
			if (ImGui.Checkbox(Ktisis.Locale.Translate("popups.world_filters.actors"), ref actors))
				ctx.Config.Overlay.ActiveWorldFilters ^= WorldFilters.Actors;
		using (ImRaii.PushColor(ImGuiCol.Text, ctx.Config.Overlay.LightNodeColor))
			if (ImGui.Checkbox(Ktisis.Locale.Translate("popups.world_filters.lights"), ref lights))
				ctx.Config.Overlay.ActiveWorldFilters ^= WorldFilters.Lights;
	}
}
