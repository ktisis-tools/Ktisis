using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Interface.Gui;

namespace Ktisis.Services; 

[DIService]
public class CommandService : IDisposable {
	// Service

	private readonly ICommandManager _cmd;
	private readonly PluginGui _gui;
	
	public CommandService(
		ICommandManager _cmd,
		PluginGui _gui,
		InitEvent _init
	) {
		this._cmd = _cmd;
		this._gui = _gui;
		
		_init.Subscribe(Initialize);
	}

	private void Initialize() {
		this.AddHandler("/ktisis", new CommandInfo(OnCommand) {
			HelpMessage = "Toggle the Ktisis GUI."
		});
	}
	
	// State
	
	private readonly Dictionary<string, CommandInfo> Commands = new();
	
	private void AddHandler(string name, CommandInfo cmd) {
		Ktisis.Log.Verbose($"Registering command handler for '{name}'");
		if (this._cmd.AddHandler(name, cmd))
			this.Commands.Add(name, cmd);
		else
			Ktisis.Log.Warning("Failed to register command.");
	}
	
	// Main command handler
	
	private void OnCommand(string _name, string _args) {
		var split = _args.Split(" ");
		switch (split[0]) {
			default:
				this._gui.ToggleMainWindow();
				break;
		}
	}
	
	// Disposal

	public void Dispose() {
		Ktisis.Log.Verbose("Disposing commands...");
		this.Commands.Keys.ToList().ForEach(RemoveHandler);
		this.Commands.Clear();
	}
	
	private void RemoveHandler(string name) {
		if (this._cmd.RemoveHandler(name))
			Ktisis.Log.Verbose($"Removed command handler for '{name}'.");
		else
			Ktisis.Log.Warning($"Failed to remove command handler for '{name}'!");
	}
}
