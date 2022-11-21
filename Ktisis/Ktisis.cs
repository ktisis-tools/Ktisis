using System;

using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Interface;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Structs.Actor.State;
using Ktisis.Structs.Actor;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static Configuration Configuration { get; private set; } = null!;

		public static bool IsInGPose => Services.PluginInterface.UiBuilder.GposeActive;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? Services.ObjectTable.CreateObjectReference((IntPtr)Services.Targets->GPoseTarget) : null;
		public unsafe static Actor* Target => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;
		public Ktisis(DalamudPluginInterface pluginInterface) {
			Services.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Configuration.Validate();

			// Init interop stuff

			Interop.Alloc.Init();
			Interop.Methods.Init();
			Interop.StaticOffsets.Init();

			Interop.Hooks.ActorHooks.Init();
			Interop.Hooks.ControlHooks.Init();
			Interop.Hooks.EventsHooks.Init();
			Interop.Hooks.GuiHooks.Init();
			Interop.Hooks.PoseHooks.Init();

			Input.Init();
			ActorStateWatcher.Init();

			// Register command

			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			// Overlays & UI

			if (Configuration.AutoOpenCtor)
				Workspace.Show();

			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += KtisisGui.Draw;
		}

		public void Dispose() {
			Services.CommandManager.RemoveHandler(CommandName);
			Services.PluginInterface.SavePluginConfig(Configuration);

			Interop.Hooks.ActorHooks.Dispose();
			Interop.Hooks.ControlHooks.Dispose();
			Interop.Hooks.EventsHooks.Dispose();
			Interop.Hooks.GuiHooks.Dispose();
			Interop.Hooks.PoseHooks.Dispose();

			Interop.Alloc.Dispose();
			Input.Instance.Dispose();
			ActorStateWatcher.Instance.Dispose();

			GameData.Sheets.Cache.Clear();
			if (EditEquip.Items != null)
				EditEquip.Items = null;
		}

		private void OnCommand(string command, string arguments) {
			Workspace.Show();
		}
	}
}
