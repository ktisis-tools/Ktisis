using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Ktisis.Core.Attributes;

namespace Ktisis.Interop;

[Singleton]
public class DllResolver : IDisposable {
	private readonly List<nint> Handles = new();
	private AssemblyLoadContext? Context;

	public void Create() {
		Ktisis.Log.Debug("Creating DLL resolver for unmanaged libraries");

		this.Context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
		if (this.Context != null)
			this.Context.ResolvingUnmanagedDll += this.ResolveUnmanaged;
	}

	private nint ResolveUnmanaged(Assembly assembly, string library) {
		var loc = Path.GetDirectoryName(assembly.Location);
		if (loc == null)
			return nint.Zero;

		var path = Path.Combine(loc, library);
		Ktisis.Log.Debug($"Resolving native assembly path: {path}");

		if (NativeLibrary.TryLoad(path, out var handle) && handle != nint.Zero) {
			this.Handles.Add(handle);
			Ktisis.Log.Debug($"Success, resolved library handle: {handle:X}");
		} else {
			Ktisis.Log.Warning($"Failed to resolve native assembly path: {path}");
		}

		return handle;
	}

	public void Dispose() {
		Ktisis.Log.Debug("Disposing DLL resolver for unmanaged libraries");

		if (this.Context != null)
			this.Context.ResolvingUnmanagedDll -= this.ResolveUnmanaged;
		this.Context = null;
		
		this.Handles.ForEach(this.FreeHandle);
		this.Handles.Clear();
	}

	private void FreeHandle(nint handle) {
		Ktisis.Log.Debug($"Freeing library handle: {handle:X}");
		NativeLibrary.Free(handle);
	}
}
