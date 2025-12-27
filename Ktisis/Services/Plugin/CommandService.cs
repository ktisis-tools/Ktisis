using System;
using System.Collections.Generic;

using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using HandlerDelegate = Dalamud.Game.Command.IReadOnlyCommandInfo.HandlerDelegate;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context;
using Ktisis.Interface;

namespace Ktisis.Services.Plugin;

[Singleton]
public class CommandService : IDisposable {
    private readonly ICommandManager _cmd;
    private readonly IChatGui _chat;
    private readonly ContextManager _ctx;
    private readonly GuiManager _gui;
    private readonly IClientState _client;
    private readonly ContextBuilder _builder;

    private readonly HashSet<string> _register = new();

    public CommandService(
        ICommandManager cmd,
        IChatGui chat,
        ContextManager ctx,
        GuiManager gui,
        IClientState client,
        ContextBuilder builder
    ) {
        this._cmd = cmd;
        this._chat = chat;
        this._ctx = ctx;
        this._gui = gui;
        this._client = client;
        this._builder = builder;
    }

    // Handler registration

    public void RegisterHandlers() {
        this.BuildCommand("/ktisis", this.OnMainCommand)
            .SetMessage("Toggle the main Ktisis window.")
            .Create();
    }

    private void Add(string name, CommandInfo info) {
        if (this._register.Add(name))
            this._cmd.AddHandler(name, info);
    }

    private CommandFactory BuildCommand(string name, HandlerDelegate handler)
        => new(this, name, handler);

    // Command handlers

    private void OnMainCommand(string command, string arguments) {
        Ktisis.Log.Info("Main command used");

        var ctx = this._ctx.Current;

        if (this._client.IsGPosing == false) {
            this._chat.PrintError("Cannot open Ktisis workspace outside of GPose.");
            return;
        }
        if (this._ctx.Current == null) {
            this._ctx.SetupEditor();
        }

        if (arguments.Contains("debug")) {
            Ktisis.Log.Info("Debug argument provided");
            ctx?.Interface.ToggleDebugWindow();
            return;
        }

        ctx?.Interface.ToggleWorkspaceWindow();
    }

	// Disposal

	public void Dispose() {
		foreach (var cmdName in this._register)
			this._cmd.RemoveHandler(cmdName);
	}

	// Factory

	private class CommandFactory {
		private readonly CommandService _cmd;

		private readonly string Name;
		private readonly List<string> Alias = new();

		private readonly HandlerDelegate Handler;

		private bool ShowInHelp;
		private string HelpMessage = string.Empty;

		public CommandFactory(
			CommandService cmd,
			string name,
			HandlerDelegate handler
		) {
			this._cmd = cmd;
			this.Name = name;
			this.Handler = handler;
		}

		// Factory methods

		public CommandFactory SetMessage(string message) {
			this.ShowInHelp = true;
			this.HelpMessage = message;
			return this;
		}

		public CommandFactory AddAlias(string alias) {
			this.Alias.Add(alias);
			return this;
		}

		public CommandFactory AddAliases(params string[] aliases) {
			this.Alias.AddRange(aliases);
			return this;
		}

		public void Create() {
			this._cmd.Add(this.Name, this.BuildCommandInfo());
			this.Alias.ForEach(this.CreateAlias);
		}

		private void CreateAlias(string alias) {
			this._cmd.Add(alias, new CommandInfo(this.Handler) {
				ShowInHelp = false
			});
		}

		// CommandInfo

		private CommandInfo BuildCommandInfo() {
			var message = this.HelpMessage;
			if (this.HelpMessage != string.Empty && this.Alias.Count > 0) {
				var padding = new string(' ', this.Name.Length * 2);
				message += $"\n{padding} (Aliases: {string.Join(", ", this.Alias)})";
			}

			return new CommandInfo(this.Handler) {
				ShowInHelp = this.ShowInHelp,
				HelpMessage = message
			};
		}
	}
}
