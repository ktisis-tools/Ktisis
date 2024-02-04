using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using Ktisis.Core.Attributes;
using Ktisis.Structs.Vfx.Apricot;

namespace Ktisis.Services.Game;

[Singleton]
public class VfxService {
	public VfxService(
		IGameInteropProvider interop
	) {
		interop.InitializeFromAttributes(this); // TODO
	}

	public unsafe ApricotCore* GetApricotCore() => this.GetApricotCoreFunc != null ? this.GetApricotCoreFunc() : null;

	[Signature("E8 ?? ?? ?? ?? 48 8B 14 1E")]
	private GetApricotCoreDelegate? GetApricotCoreFunc = null;
	private unsafe delegate ApricotCore* GetApricotCoreDelegate();
}
