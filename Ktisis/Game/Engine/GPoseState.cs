using JetBrains.Annotations;

using Dalamud.Game;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;
using Ktisis.Interop.Structs.Event;

namespace Ktisis.Game.Engine;

public delegate void GPoseEvent(GPoseState sender, bool isActive);

public class GPoseState : IEventClient {
	// Dependency access

	private GameService GameService;
	
	// Target system
	
	private unsafe TargetSystem* Target = null;
	
	private unsafe nint TargetAddress
		=> Target != null ? (nint)Target->GPoseTarget : 0;
	
	// GPose state
	
	public bool Active { get; private set; }

	private bool GetActive()
		=> Ktisis.PluginApi.UiBuilder.GposeActive && TargetAddress != 0;
	
	// Constructor

	internal unsafe GPoseState(GameService game) {
		GameService = game;

		Target = TargetSystem.Instance();
		
		Services.Interop.Methods.Resolve(this);
	}

	// GPose event

	[EventEmitter, UsedImplicitly]
	private event GPoseEvent? GPoseEvent;

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
	
	// GPoseModule
	
	private unsafe delegate nint GPoseModuleDelegate(EventFramework* events);

	[Signature("E8 ?? ?? ?? ?? 0F B7 57 3C"), UsedImplicitly]
	private GPoseModuleDelegate? GetGPoseModuleAddress;

	internal unsafe GPoseModule* GetEventModule() {
		var events = EventFramework.Instance();
		var addr = events != null ? GetGPoseModuleAddress?.Invoke(events) : null;
		return (GPoseModule*)(addr ?? nint.Zero);
	}

	// Disposal

	public void Dispose() {
		Active = false;
	}
}