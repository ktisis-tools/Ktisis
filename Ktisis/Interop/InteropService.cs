using System;

using Ktisis.Interop.Unmanaged;

namespace Ktisis.Interop; 

public class InteropService : IDisposable {
	// Service

	private readonly DllResolver DllResolver;

	public InteropService() {
		this.DllResolver = new DllResolver();
	}
	
	// Disposal

	private bool IsDisposed;
	
	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		this.DllResolver.Dispose();
	}
}