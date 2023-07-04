using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Dalamud.Logging;

namespace Ktisis.Interop.Native;

// Temporary workaround as loading unmanaged DLLs in Dalamud is currently bugged.
// See the following issue: https://github.com/goatcorp/Dalamud/issues/1238
// Thanks to Minoost for providing this method.

internal class DllResolver {
	private static AssemblyLoadContext? Context;

	private static nint Handle;

	internal static void Init() {
		Context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
		if (Context != null)
			Context.ResolvingUnmanagedDll += ResolveUnmanaged;
	}

	internal static void Dispose() {
		if (Context != null)
			Context.ResolvingUnmanagedDll -= ResolveUnmanaged;
		Context = null;

		if (Handle != nint.Zero)
			NativeLibrary.Free(Handle);
		Handle = nint.Zero;
	}

	private static nint ResolveUnmanaged(Assembly assembly, string library) {
		var loc = Path.GetDirectoryName(assembly.Location);
		if (loc == null) return nint.Zero;

		var path = Path.Combine(loc, library);
		PluginLog.Information($"Resolved native assembly path: {path}");

		return Handle = NativeLibrary.TryLoad(path, out var handle) ? handle : nint.Zero;
	}
}
