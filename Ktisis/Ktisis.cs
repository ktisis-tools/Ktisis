using System.Numerics;

using ImGuiNET;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game.Command;

using Ktisis.Overlay;
using Ktisis.Interface;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static Configuration Configuration { get; private set; } = null!;

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

			Workspace.Show();

			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += KtisisGui.Draw;
		}

		public void Dispose() {
			// TODO
			Dalamud.CommandManager.RemoveHandler(CommandName);
			Dalamud.PluginInterface.SavePluginConfig(Configuration);
		}

		private void OnCommand(string command, string arguments) {
			Workspace.Show();
		}
	}
}
