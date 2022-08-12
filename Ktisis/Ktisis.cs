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
using Ktisis.Interface;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";

		public Configuration Configuration;

		internal KtisisUI Interface;
		internal SkeletonEditor SkeletonEditor { get; init; }

		internal DalamudPluginInterface PluginInterface { get; init; }
		internal CommandManager CommandManager { get; init; }
		internal ClientState ClientState { get; init; }
		internal ObjectTable ObjectTable { get; init; }
		internal SigScanner SigScanner { get; init; }
		internal GameGui GameGui { get; init; }

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

			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Interface = new KtisisUI(this);
			SkeletonEditor = new SkeletonEditor(this, null);

			Interface.Show();

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

			Interface.Draw();

			SkeletonEditor.Draw(draw);

			ImGui.End();
			ImGui.PopStyleVar();
		}

		public bool IsInGpose() {
			return PluginInterface.UiBuilder.GposeActive;
		}
	}
}
