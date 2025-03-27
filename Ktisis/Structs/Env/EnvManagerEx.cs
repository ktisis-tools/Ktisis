using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

namespace Ktisis.Structs.Env;

[StructLayout(LayoutKind.Explicit, Size = 0x910)]
public struct EnvManagerEx {
	[FieldOffset(0x000)] public EnvManager _base;
	
	[FieldOffset(0x058)] public EnvState EnvState;

	[FieldOffset(0x4E0)] public EnvSimulator EnvSimulator;

	public unsafe static EnvManagerEx* Instance() => (EnvManagerEx*)EnvManager.Instance();
}
