using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Overlay;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }
		private ClientState ClientState { get; init; }
		private ObjectTable ObjectTable { get; init; }

		private Skeleton SkeletonOverlay { get; init; }

		public Ktisis(
			DalamudPluginInterface pluginInterface,
			CommandManager cmdManager,
			ClientState clientState,
			ObjectTable objTable,
			GameGui gameGui
		) {
			PluginInterface = pluginInterface;
			CommandManager = cmdManager;
			ClientState = clientState;
			ObjectTable = objTable;

			SkeletonOverlay = new Skeleton(gameGui, null);

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

			var tarSys = TargetSystem.Instance();
			if (tarSys != null) {
				SkeletonOverlay.Subject = ObjectTable.CreateObjectReference((IntPtr)(tarSys->GPoseTarget));
				SkeletonOverlay.Draw(draw);
			}

			ImGui.End();
			ImGui.PopStyleVar();
		}
	}
}
