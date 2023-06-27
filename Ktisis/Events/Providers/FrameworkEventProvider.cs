using JetBrains.Annotations;

using Dalamud.Game;

using Ktisis.Core;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;

namespace Ktisis.Events.Providers;

public delegate void FrameworkEvent(Framework framework);

public class FrameworkEventProvider : EventProvider {
	[EventEmitter, UsedImplicitly]
	private event FrameworkEvent? FrameworkEvent;

	public override void Setup() {
		Services.Framework.Update += OnFrameworkUpdate;
	}

	private void OnFrameworkUpdate(Framework framework) {
		if (!Services.Ready || IsDisposed) return;
		FrameworkEvent?.InvokeSafely(framework);
	}

	protected override void OnDispose() {
		Services.Framework.Update -= OnFrameworkUpdate;
	}
}
