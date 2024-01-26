using System;

using Ktisis.Core.Attributes;
using Ktisis.Interface;
using Ktisis.Legacy.Interface;
using Ktisis.Services;
using Ktisis.Services.Game;

namespace Ktisis.Legacy;

[Singleton]
public class LegacyMigrator {
	private readonly GPoseService _gpose;
	private readonly GuiManager _gui;

	public event Action? OnConfirmed;
	
	public LegacyMigrator(
		GPoseService gpose,
		GuiManager gui
	) {
		this._gpose = gpose;
		this._gui = gui;
	}
	
	// Setup

	public void Setup() {
		Ktisis.Log.Warning("User is migrating from Ktisis v0.2, activating legacy mode.");
		this._gpose.StateChanged += this.OnGPoseStateChanged;
		this._gpose.Subscribe();
	}

	private void OnGPoseStateChanged(object sender, bool state) {
		if (!state || this._confirmed) return;
		var window = this._gui.GetOrCreate<MigratorWindow>(this);
		window.Open();
	}
	
	// Begin from UI

	private bool _confirmed;
	
	public void Begin() {
		if (this._confirmed) return;
		this._confirmed = true;
		this._gpose.StateChanged -= this.OnGPoseStateChanged;
		this._gpose.Reset();
		this.OnConfirmed?.Invoke();
	}
}
