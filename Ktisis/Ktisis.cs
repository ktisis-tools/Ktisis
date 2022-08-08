using System;
using System.IO;
using System.Reflection;
using System.Numerics;

using ImGuiNET;

using Dalamud;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;

using Ktisis.Overlay;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }
		private ClientState ClientState { get; init; }

		private Skeleton SkeletonOverlay { get; init; }

		public Ktisis(
			DalamudPluginInterface pluginInterface,
			CommandManager cmdManager,
			ClientState clientState,
			GameGui gameGui
		) {
			PluginInterface = pluginInterface;
			CommandManager = cmdManager;
			ClientState = clientState;

			SkeletonOverlay = new Skeleton(gameGui, null);

			pluginInterface.UiBuilder.Draw += Draw;
		}

		public void Dispose() {
			// TODO
		}

		public unsafe void Draw() {
			var actor = ClientState.LocalPlayer;
			if (actor == null)
				return;

			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			//SkeletonOverlay.Subject = actor;
			SkeletonOverlay.Draw();

			var draw = ImGui.GetWindowDrawList();

			ImGui.End();
			ImGui.PopStyleVar();
		}
	}
}
