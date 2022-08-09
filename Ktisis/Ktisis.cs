using System.Numerics;

using ImGuiNET;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;

using Ktisis.Overlay;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";

		internal DalamudPluginInterface PluginInterface { get; init; }
		internal CommandManager CommandManager { get; init; }
		internal ClientState ClientState { get; init; }
		internal ObjectTable ObjectTable { get; init; }
		internal SigScanner SigScanner { get; init; }
		internal GameGui GameGui { get; init; }

		private SkeletonEditor SkeletonEditor { get; init; }

		public Ktisis(
			DalamudPluginInterface pluginInterface,
			CommandManager cmdManager,
			ClientState clientState,
			ObjectTable objTable,
			SigScanner sigScanner,
			GameGui gameGui
		) {
			PluginInterface = pluginInterface;
			CommandManager = cmdManager;
			ClientState = clientState;
			ObjectTable = objTable;
			SigScanner = sigScanner;
			GameGui = gameGui;

			SkeletonEditor = new SkeletonEditor(this, null);

			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += Draw;
		}

		public void Dispose() {
			// TODO
		}

		public unsafe void Draw() {
			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			var draw = ImGui.GetWindowDrawList();

			/*var tarSys = TargetSystem.Instance();
			if (tarSys != null) {
				SkeletonEditor.Subject = ObjectTable.CreateObjectReference((IntPtr)(tarSys->GPoseTarget));
				SkeletonEditor.Draw(draw);
			}*/
			SkeletonEditor.Draw(draw);

			ImGui.End();
			ImGui.PopStyleVar();
		}
	}
}
