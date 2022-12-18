using System;

using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Structs.Actor.State;
using Ktisis.Structs.Actor;
using Ktisis.History;
using Ktisis.Events;
using Ktisis.Overlay;
using Dalamud.Logging;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static string Version = $"Alpha {GetVersion()}";

		public static Configuration Configuration { get; private set; } = null!;
		public static UiBuilder UiBuilder { get; private set; } = null!;

		public static bool IsInGPose => Services.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();
		public unsafe static bool IsGposeTargetPresent() => (IntPtr)Services.Targets->GPoseTarget != IntPtr.Zero;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? Services.ObjectTable.CreateObjectReference((IntPtr)Services.Targets->GPoseTarget) : null;
		public unsafe static Actor* Target => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;

		public static string GetVersion() {
			return typeof(Ktisis).Assembly.GetName().Version!.ToString(fieldCount: 3);
		}

		public Ktisis(DalamudPluginInterface pluginInterface) {
			Services.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			UiBuilder = pluginInterface.UiBuilder;

			if (Configuration.IsFirstTimeInstall) {
				Configuration.IsFirstTimeInstall = false;
				Information.Show();
			}
			if (Configuration.LastPluginVer != Version) {
				Configuration.LastPluginVer = Version;
			}

			Configuration.Validate();

			// Init interop stuff

			EventManager.OnGPoseChange += Workspace.OnEnterGposeToggle; // must be placed before ActorStateWatcher.GlobalInit()

			GlobalInit();

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

			OverlayWindow.DeselectGizmo();

			GlobalDispose();
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
			switch (arguments) {
				case "about":
				case "info":
				case "information":
					Information.Toggle();
					break;
				case "cfg":
				case "config":
				case "configure":
				case "configuration":
					ConfigGui.Toggle();
					break;
				default:
					Workspace.Toggle();
					break;
			}
		}

		private static void GlobalInit() {
			Interop.Alloc.GlobalInit();
			Interop.Methods.GlobalInit();
			Interop.StaticOffsets.GlobalInit();

			Interop.Hooks.ActorHooks.GlobalInit();
			Interop.Hooks.ControlHooks.GlobalInit();
			Interop.Hooks.EventsHooks.GlobalInit();
			Interop.Hooks.GuiHooks.GlobalInit();
			Interop.Hooks.PoseHooks.GlobalInit();

			Input.GlobalInit();
			ActorStateWatcher.GlobalInit();

			HistoryManager.GlobalInit();
		}

		private static void GlobalDispose() {
			HistoryManager.GlobalDispose();
			Input.GlobalDispose();
			Interop.Alloc.GlobalDispose();

			Interop.Hooks.PoseHooks.GlobalDispose();
			Interop.Hooks.GuiHooks.GlobalDispose();
			Interop.Hooks.EventsHooks.GlobalDispose();
			Interop.Hooks.ControlHooks.GlobalDispose();
			Interop.Hooks.ActorHooks.GlobalDispose();
		}
	}
}
