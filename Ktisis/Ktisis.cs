using System;

using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Localization;
using Ktisis.Structs.Actor.State;
using Ktisis.Structs.Actor;
using Ktisis.Events;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static Configuration Configuration { get; private set; } = null!;
		public static UiBuilder UiBuilder { get; private set; } = null!;

		public static bool IsInGPose => Services.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();
		public unsafe static bool IsGposeTargetPresent() => (IntPtr)Services.Targets->GPoseTarget != IntPtr.Zero;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? Services.ObjectTable.CreateObjectReference((IntPtr)Services.Targets->GPoseTarget) : null;
		public unsafe static Actor* Target => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;

		public Ktisis(DalamudPluginInterface pluginInterface) {
			Services.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			UiBuilder = pluginInterface.UiBuilder;

			Configuration.Validate();

			/* Load the current localization */
			Locale.LoadLocale(Configuration.Localization);

			// Init interop stuff

			Interop.Alloc.Init();
			Interop.Methods.Init();
			Interop.StaticOffsets.Init();

			Interop.Hooks.ActorHooks.Init();
			Interop.Hooks.ControlHooks.Init();
			Interop.Hooks.EventsHooks.Init();
			Interop.Hooks.GuiHooks.Init();
			Interop.Hooks.PoseHooks.Init();

			EventManager.OnGPoseChange += Workspace.OnEnterGposeToggle; // must be placed before ActorStateWatcher.Init()

			Input.Init();
			ActorStateWatcher.Init();

			// Register command

			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			// Overlays & UI

			if (Configuration.OpenKtisisMethod == OpenKtisisMethod.OnPluginLoad)
				Workspace.Show();

			pluginInterface.UiBuilder.OpenConfigUi += ConfigGui.Toggle;
			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += KtisisGui.Draw;

			References.LoadReferences(Configuration);
		}

		public void Dispose() {
			Services.CommandManager.RemoveHandler(CommandName);
			Services.PluginInterface.SavePluginConfig(Configuration);
			Services.PluginInterface.UiBuilder.OpenConfigUi -= ConfigGui.Toggle;

			Interop.Hooks.ActorHooks.Dispose();
			Interop.Hooks.ControlHooks.Dispose();
			Interop.Hooks.EventsHooks.Dispose();
			Interop.Hooks.GuiHooks.Dispose();
			Interop.Hooks.PoseHooks.Dispose();

			Interop.Alloc.Dispose();
			Input.Instance.Dispose();
			ActorStateWatcher.Instance.Dispose();
			EventManager.OnGPoseChange -= Workspace.OnEnterGposeToggle;

			Data.Sheets.Cache.Clear();

			if (EditEquip.Items != null)
				EditEquip.Items = null;

			foreach (var (_, texture) in References.Textures) {
				texture.Dispose();
			}
		}

		private void OnCommand(string command, string arguments) {
			Workspace.Show();
		}
	}
}