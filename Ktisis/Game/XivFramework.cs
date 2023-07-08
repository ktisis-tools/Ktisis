using Dalamud.Game;

using JetBrains.Annotations;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;

namespace Ktisis.Game; 

public delegate void FrameworkEvent(Framework framework);

public class XivFramework : EventProvider {
	// Event provider
	
	[EventEmitter, UsedImplicitly]
	private event FrameworkEvent? FrameworkEvent;

	// Provider setup
	
	public override void Setup() {
		Services.Framework.Update += OnFrameworkUpdate;
	}
	
	// Framework event handler
	
	private void OnFrameworkUpdate(Framework framework) {
		if (!Services.Ready || IsDisposed) return;
		FrameworkEvent?.InvokeSafely(framework);
	}

	// Disposal

	protected override void OnDispose() {
		Services.Framework.Update -= OnFrameworkUpdate;
	}
}