using JetBrains.Annotations;

using Dalamud.Game;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Providers;
using Ktisis.Events.Attributes;
using Ktisis.Extensions;

namespace Ktisis.Core.Providers;

public delegate void GPoseEvent(GPoseService sender, bool isActive);

public class GPoseService : Service, IEventClient {
	// Instance of native TargetSystem singleton
	private unsafe TargetSystem* Target = null;

	// Public GPose state after processing changes.
	public bool Active { get; private set; }

	// Gets address of GPose target from TargetSystem.
	private unsafe nint TargetAddress
		=> Target != null ? (nint)Target->GPoseTarget : 0;

	// Checks current GPose state.
	private bool GetActive()
		=> Services.PluginInterface.UiBuilder.GposeActive && TargetAddress != 0;

	// Emitters

	[EventEmitter, UsedImplicitly]
	private event GPoseEvent? GPoseEvent;

	// Initialize

	public unsafe override void Init() {
		Target = TargetSystem.Instance();
	}

	// Framework event

	[UsedImplicitly]
	[Listener<FrameworkEvent>]
	private void OnFrameworkUpdate(Framework _) {
		var active = false;
		try {
			active = GetActive();
			if (active == Active) return;
			Services.Conditions[Condition.IsInGPose] = active;
			GPoseEvent?.InvokeSafely(this, active);
		} finally {
			Active = active;
		}
	}

	// Disposal

	public override void Dispose() {
		Active = false;
	}
}
