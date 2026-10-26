using System;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Event;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Events;

namespace Ktisis.Services.Game;

public delegate void GPoseStateHandler(GPoseService sender, bool state);

[Singleton]
public class GPoseService : IDisposable {
	private readonly IClientState _clientState;
	private readonly IFramework _framework;
	private readonly ITargetManager _targets;
	private readonly ConfigManager _cfg;
	
	private readonly Event<Action> _updateEvent;
	public event Action Update {
		add => this._updateEvent.Add(value);
		remove => this._updateEvent.Remove(value);
	}

	private readonly Event<Action<GPoseService, bool>> _gposeEvent;
	public event GPoseStateHandler StateChanged {
		add => this._gposeEvent.Add(value.Invoke);
		remove => this._gposeEvent.Remove(value.Invoke);
	}
	
	private bool _isActive;

	public bool IsGPosing => this._clientState.IsGPosing;

	public IGameObject? GPoseTarget => this._targets.GPoseTarget;
	
	public GPoseService(
		IClientState clientState,
		IFramework framework,
		ITargetManager targets,
		Event<Action> updateEvent,
		Event<Action<GPoseService, bool>> gposeEvent,
		ConfigManager cfg
	) {
		this._clientState = clientState;
		this._framework = framework;
		this._targets = targets;
		this._updateEvent = updateEvent;
		this._gposeEvent = gposeEvent;
		this._cfg = cfg;
		this.StateChanged +=(this.EditVanillaGposeSettings);
	}

	private bool _isSubscribed;

	public void Subscribe() {
		if (this._isSubscribed) return;
		this._framework.Update += this.OnFrameworkUpdate;
		this._isSubscribed = true;
	}

	public void Reset() => this._isActive = false;

	private void OnFrameworkUpdate(IFramework sender) {
		var state = this.IsGPosing;
		if (this._isActive != state) {
			this._isActive = state;
			Ktisis.Log.Info($"GPose state changed: {state}");
			this._gposeEvent.Invoke(this, state);
		}

		if (state) this._updateEvent.Invoke();
	}
	
	private unsafe void EditVanillaGposeSettings(GPoseService sender, bool state) {
		if (state) {
			var gpose = EventFramework.Instance();
			if (this._cfg.GetConfigFileExists()) {
				if (this._cfg is { _isLoaded: true, File.Editor.DisableCameraDofStartup: true }) {
					byte* dof = (byte*)(((nint)gpose) + 0x312);
					*dof &= 0xFD;
				}
				/*if (this._cfg is { _isLoaded: true, File.Editor.DisableAnimationLoop: true }) { // this gets reset, gotta figure it out
					this._framework.DelayTicks(100).ContinueWith((a) => {
						byte* anim = (byte*)(((nint)gpose) + 0x691);
						*anim |= 0x1;
					});

				}*/
			}
		}
	}

	public void Dispose() {
		this._framework.Update -= this.OnFrameworkUpdate;
		this._isSubscribed = false;
	}
}
