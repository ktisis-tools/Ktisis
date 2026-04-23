using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using FFXIVClientStructs;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Types;
using Ktisis.Localization;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Windows.ToolbarModules;

public class Pose : PosingWindow{
	

	protected readonly IEditorContext _ctx;
	protected ObjectWindow _subWindow;
	public Pose(
		IEditorContext ctx,
		ITextureProvider tex,
		LocaleManager locale,
		GPoseService gpose
	) : base(
		ctx,
		tex,
		locale,
		gpose) {
		this._ctx = ctx;
	}
	public override void PreDraw() {
		this.Size = Vector2.Zero;
		base.PreDraw();
	}
	public override void Draw() {

		using (var _child = ImRaii.Group()) {
			this._subWindow = this._ctx.Interface.GetObjectWindow();
			this._subWindow.OnOpen();
			base.Draw();
		}

		if (this._target is { IsValid: true }) {
			ImGui.SameLine();
			using var _ = ImRaii.Group();
			this._subWindow.DrawCompact();
		}
	}

}
