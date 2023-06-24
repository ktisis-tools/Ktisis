using Dalamud.Game;

using Ktisis.Core;
using Ktisis.Events.Attributes;

namespace Ktisis.Events.Providers;

public delegate void FrameworkEvent(Framework framework);

public class FrameworkEventProvider : EventProvider {
	[EventEmitter]
	private event FrameworkEvent? Event;

	public override void Setup() {
		Services.Framework.Update += OnFrameworkUpdate;
	}

	private void OnFrameworkUpdate(Framework framework) {
		if (!Services.Ready || IsDisposed) return;
		Event?.Invoke(framework);
	}

	protected override void OnDispose() {
		Services.Framework.Update -= OnFrameworkUpdate;
	}
}
