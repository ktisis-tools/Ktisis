using System;

using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Plugin.Services;

using Ktisis.Camera;
using Ktisis.Env;
using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Structs.Actor.State;
using Ktisis.Structs.Actor;
using Ktisis.History;
using Ktisis.Events;
using Ktisis.Overlay;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static string Version = $"Alpha {GetVersion()}";

		public static Configuration Configuration { get; private set; } = null!;
		public static IUiBuilder UiBuilder { get; private set; } = null!;
		public static IPluginLog Log { get; private set; } = null!;

		public static bool IsInGPose => Services.ClientState.IsGPosing && IsGposeTargetPresent();
		public unsafe static bool IsGposeTargetPresent() => (IntPtr)Services.Targets->GPoseTarget != IntPtr.Zero;

		public unsafe static IGameObject? GPoseTarget
			=> IsInGPose ? Services.ObjectTable.CreateObjectReference((IntPtr)Services.Targets->GPoseTarget) : null;
		public unsafe static Actor* Target => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;

		public static string GetVersion() {
			return typeof(Ktisis).Assembly.GetName().Version!.ToString(fieldCount: 3);
		}

		public Ktisis(
			IDalamudPluginInterface pluginInterface,
			IPluginLog log
		) {
			Services.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			UiBuilder = pluginInterface.UiBuilder;
			Log = log;

			if (Configuration.IsFirstTimeInstall) {
				Configuration.IsFirstTimeInstall = false;
				Information.Show();
			}
			if (Configuration.LastPluginVer != Version) {
				Configuration.LastPluginVer = Version;
			}

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

			CameraService.Init();
			EnvService.Init();
			
			EventManager.OnGPoseChange += Workspace.OnEnterGposeToggle; // must be placed before ActorStateWatcher.Init()

			Input.Init();
			ActorStateWatcher.Init();
			ActorWetnessOverride.Instance.Init();

			// Register command

			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			// Overlays & UI

			if (Configuration.OpenKtisisMethod == OpenKtisisMethod.OnPluginLoad)
				Workspace.Show();

			pluginInterface.UiBuilder.OpenMainUi += Workspace.Toggle;
			pluginInterface.UiBuilder.OpenConfigUi += ConfigGui.Toggle;
			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += KtisisGui.Draw;

			HistoryManager.Init();
			References.LoadReferences(Configuration);
		}

		public void Dispose() {
			Services.CommandManager.RemoveHandler(CommandName);
			Services.PluginInterface.SavePluginConfig(Configuration);
			Services.PluginInterface.UiBuilder.OpenMainUi -= Workspace.Toggle;
			Services.PluginInterface.UiBuilder.OpenConfigUi -= ConfigGui.Toggle;

			OverlayWindow.DeselectGizmo();

			Interop.Hooks.ActorHooks.Dispose();
			Interop.Hooks.ControlHooks.Dispose();
			Interop.Hooks.EventsHooks.Dispose();
			Interop.Hooks.GuiHooks.Dispose();
			Interop.Hooks.PoseHooks.Dispose();

			CameraService.Dispose();
			EnvService.Dispose();

			Interop.Alloc.Dispose();
			ActorStateWatcher.Dispose();
			ActorWetnessOverride.Instance.Dispose();
			EventManager.OnGPoseChange -= Workspace.OnEnterGposeToggle;

			Data.Sheets.Cache.Clear();

			Input.Dispose();
			HistoryManager.Dispose();
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
	}
}
