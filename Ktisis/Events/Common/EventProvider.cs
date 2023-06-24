using System;

namespace Ktisis.Events.Common;

public class EventProvider : IEventClient, IDisposable {
	public virtual void Setup() { }

	// Handle clean disposal

	protected bool IsDisposed;

	public void Dispose() {
		if (IsDisposed) return;
		GC.SuppressFinalize(this);
		OnDispose();
		IsDisposed = true;
	}

	protected virtual void OnDispose() { }

	~EventProvider() => Dispose();
}
