using System;

using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace Ktisis.Interop;

public class Alloc<T> : IDisposable where T : unmanaged {
	public nint Address { get; private set; }
	public unsafe T* Data => (T*)this.Address;
	
	public bool IsDisposed { get; private set; }

	public unsafe Alloc(ulong align = 8) {
		this.Address = (nint)IMemorySpace.GetDefaultSpace()->Malloc<T>(align);
	}

	public unsafe void Dispose() {
		if (this.IsDisposed) return;
		if (this.Address != nint.Zero) {
			IMemorySpace.Free(this.Data);
			this.Address = nint.Zero;
		}
		this.IsDisposed = true;
		GC.SuppressFinalize(this);
	}

	~Alloc() => this.Dispose();
}
