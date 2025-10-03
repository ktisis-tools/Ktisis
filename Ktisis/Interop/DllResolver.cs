using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Dalamud.Plugin;

using Ktisis.Core.Attributes;

namespace Ktisis.Interop;

[Singleton]
public class DllResolver : IDisposable {
	private readonly IDalamudPluginInterface _dpi;

	public DllResolver(
		IDalamudPluginInterface dpi
	) {
		this._dpi = dpi;
	}
	
	private readonly List<nint> Handles = new();
	private AssemblyLoadContext? Context;

	public void Create() {
		Ktisis.Log.Debug("Creating DLL resolver for unmanaged libraries");

		this.Context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
		if (this.Context != null)
			this.Context.ResolvingUnmanagedDll += this.ResolveUnmanaged;
	}
	
	private nint ResolveUnmanaged(Assembly assembly, string library) {
		var loc = Path.GetDirectoryName(this._dpi.AssemblyLocation.FullName);
		if (loc == null) {
			Ktisis.Log.Warning("Failed to resolve location for native assembly!");
			return nint.Zero;
		}

		// Try with .dll extension if not present
		if (!library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
			library += ".dll";
		}

		var path = Path.Combine(loc, library);
		Ktisis.Log.Debug($"Resolving native assembly path: {path}");
		Ktisis.Log.Debug($"File exists: {File.Exists(path)}");

		try {
			// Use Load instead of TryLoad to get exception details
			var handle = NativeLibrary.Load(path);
			this.Handles.Add(handle);
			Ktisis.Log.Debug($"Success, resolved library handle: {handle:X}");
			return handle;
		} catch (DllNotFoundException ex) {
			Ktisis.Log.Error( ex, $"DLL not found: {path}");
		} catch (BadImageFormatException ex) {
			Ktisis.Log.Error(ex, $"Bad image format (wrong architecture or corrupted): {path}");
		} catch (Exception ex) {
			Ktisis.Log.Error(ex, $"Failed to load native library: {path}");
		}

		return nint.Zero;
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
