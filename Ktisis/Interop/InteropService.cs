using System.Reflection;

using JetBrains.Annotations;

using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Core.Singletons;
using Ktisis.Interop.Hooking;
using Ktisis.Interop.Modules;
using Ktisis.Interop.Resolvers;

namespace Ktisis.Interop;

public class InteropService : Service, IEventClient {
	// Hooking

	private readonly DllResolver DllResolver = new();

	private readonly HookManager HookManager = new();

	internal readonly DelegateResolver Methods = new();

	// Initialization

	public override void Init() {
		DllResolver.Init();

		HookManager.Register<PoseHooks>(Condition.IsInGPose, HookFlags.RequireEvent);
		HookManager.CreateHooks();
	}

	// Hook wrappers

	internal T? GetModule<T>() where T : HookModule
		=> HookManager.GetModule<T>();

	// Events

	[UsedImplicitly]
	[Listener<ConditionEvent>]
	private void OnConditionUpdate(Condition cond, bool value)
		=> HookManager.OnConditionUpdate(cond, value);

	public void OnEventAdded(IEventClient emitter, EventInfo @event) {
		if (@event.EventHandlerType?.Name is nameof(ConditionEvent))
			HookManager.OnEventAdded();
	}

	// Dispose

	public override void Dispose() {
		DllResolver.Dispose();

		HookManager.Dispose();
	}
}
