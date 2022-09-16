using System.Numerics;

using ImGuiNET;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game.Command;

using Ktisis.Overlay;
using Ktisis.Interface;
using Ktisis.Localization;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static Configuration Configuration { get; private set; } = null!;
		internal static Locale Locale { get; private set; } = null!;

		internal KtisisGui Gui { get; init; }
		internal ConfigGui ConfigGui { get; init; }
		internal CustomizeGui CustomizeGui { get; init; }
		internal SkeletonEditor SkeletonEditor { get; init; }

		public Ktisis(
			DalamudPluginInterface pluginInterface
		) {
			Dalamud.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			// Register command

			Dalamud.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			// Overlays & UI

			Gui = new KtisisGui(this);
			ConfigGui = new ConfigGui();
			CustomizeGui = new CustomizeGui();
			SkeletonEditor = new SkeletonEditor();

			Gui.Show();

			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += Draw;
		}

		public void Dispose() {
			// TODO
			Dalamud.CommandManager.RemoveHandler(CommandName);
			Configuration.Save();
		}

		private void OnCommand(string command, string arguments) {
			Gui.Show();
		}

		public unsafe void Draw() {
			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			var draw = ImGui.GetWindowDrawList();

			Gui.Draw();
			ConfigGui.Draw();
			CustomizeGui.Draw();

			SkeletonEditor.Draw(draw);

			ImGui.End();
			ImGui.PopStyleVar();
		}

		public static bool IsInGpose() {
			return Dalamud.PluginInterface.UiBuilder.GposeActive;
		}
	}
}
