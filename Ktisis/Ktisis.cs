﻿using System;

using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static Configuration Configuration { get; private set; } = null!;

		public static bool IsInGPose => Dalamud.PluginInterface.UiBuilder.GposeActive;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? Dalamud.ObjectTable.CreateObjectReference((IntPtr)Dalamud.Targets->GPoseTarget) : null;

		public Ktisis(DalamudPluginInterface pluginInterface) {
			Dalamud.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Configuration.Validate();

			// Init interop stuff

			Interop.Alloc.Init();
			Interop.Methods.Init();
			Interop.StaticOffsets.Init();
			Interop.Hooks.ActorHooks.Init();
			Interop.Hooks.PoseHooks.Init();
			Interop.Hooks.GuiHooks.Init();
			Input.Init();

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
			Dalamud.CommandManager.RemoveHandler(CommandName);
			Dalamud.PluginInterface.SavePluginConfig(Configuration);

			Interop.Alloc.Dispose();
			Interop.Hooks.ActorHooks.Dispose();
			Interop.Hooks.PoseHooks.Dispose();
			Interop.Hooks.GuiHooks.Dispose();
			Input.Instance.Dispose();

			GameData.Sheets.Cache.Clear();
			if (EditEquip.Items != null)
				EditEquip.Items = null;
		}

		private void OnCommand(string command, string arguments) {
			Workspace.Show();
		}
	}
}
