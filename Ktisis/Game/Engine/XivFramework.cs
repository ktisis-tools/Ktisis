using JetBrains.Annotations;

using Dalamud.Game;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;

namespace Ktisis.Game.Engine;

public delegate void FrameworkEvent(Framework framework);

internal class XivFramework : EventProvider {
	// Setup

	public override void Setup() {
		Services.Framework.Update += OnFrameworkUpdate;
	}

	// Framework event provider

	[EventEmitter, UsedImplicitly]
	private event FrameworkEvent? FrameworkEvent;

	private void OnFrameworkUpdate(Framework framework) {
		if (!Services.Ready || IsDisposed) return;
		FrameworkEvent?.InvokeSafely(framework);
	}

	// Disposal

	protected override void OnDispose() {
		Services.Framework.Update -= OnFrameworkUpdate;
	}
}
