using Dalamud.Game;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Common;
using Ktisis.Events.Attributes;

namespace Ktisis.Providers;

public delegate void GPoseEvent(GPoseService sender, bool isActive);

public class GPoseService : Service, IEventClient {
	// Instance of native TargetSystem singleton
	private unsafe TargetSystem* Target = null;
	
	// Public GPose state after processing changes.
	public bool Active { get; private set; }
	
	// Gets address of GPose target from TargetSystem.
	private unsafe nint TargetAddress => Target != null ? (nint)Target->GPoseTarget : 0;
	
	// Checks current GPose state.
	private bool GetActive() => Services.PluginInterface.UiBuilder.GposeActive && TargetAddress != 0;
	
	// Emitters

	[EventEmitter]
	private event GPoseEvent? GPoseEvent;

	// Initialize
	
	public unsafe override void Init() {
		Target = TargetSystem.Instance();
	}
	
	// Framework event

	[Listener<FrameworkEvent>]
	private void OnFrameworkUpdate(Framework _) {
		var active = GetActive();
		if (active == Active) return;
		Active = active;
		GPoseEvent?.Invoke(this, Active);
	}

	// Disposal

	public override void Dispose() {
		Active = false;
	}
}