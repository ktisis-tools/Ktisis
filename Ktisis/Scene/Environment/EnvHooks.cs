using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using Ktisis.Interop.Hooking;
using Ktisis.Interop.Structs.Scene;

namespace Ktisis.Scene.Environment;

public class EnvHooks : HookContainer {
	// Overrides
	// TODO TODO TODO

	public readonly EnvOverride Overrides = new();
	
	public class EnvOverride {
		public bool Advanced = true;
		
		public EnvProps Props;
	}
	
	public unsafe void CopyOverride() {
		var env = EnvManager.Instance();
		if (env != null) CopyOverride((EnvProps*)env);
	}

	private unsafe void CopyOverride(EnvProps* env)
		=> this.Overrides.Props = *env;

	private unsafe void ApplyOverride(EnvProps* env) {
		// TODO: Reflection

		env->Time = this.Overrides.Props.Time;
		if (this.Overrides.Advanced)
			env->SkyId = this.Overrides.Props.SkyId;
	}
	
	// EnvManager Update
	
	[Signature("E8 ?? ?? ?? ?? 49 8B 0E 48 8D 93 ?? ?? ?? ??")]
	private Hook<EnvUpdateDelegate> EnvUpdateHook = null!;
	
	private unsafe delegate nint EnvUpdateDelegate(EnvManager* env, nint a2);

	public unsafe nint EnvUpdateDetour(EnvManager* env, nint a2) {
		env->DayTimeSeconds = this.Overrides.Props.Time;
		return this.EnvUpdateHook.Original(env, a2);
	}
	
	// Skybox Update

	[Signature("E8 ?? ?? ?? ?? 0F 28 74 24 ?? C6 43 30 00")]
	private Hook<SkyboxUpdateDelegate> SkyboxUpdateHook = null!;

	private unsafe delegate nint SkyboxUpdateDelegate(EnvManager* env, float a2, float a3);

	private unsafe nint SkyboxUpdateDetour(EnvManager* env, float a2, float a3) {
		ApplyOverride((EnvProps*)env);
		return this.SkyboxUpdateHook.Original(env, a2, a3);
	}
	
	// Skybox Texture

	[Signature("E8 ?? ?? ?? ?? 44 38 63 30 74 05 0F 28 DE", DetourName = "SkyTexDetour")]
	private Hook<SkyTexDelegate> SkyTexHook = null!;

	private delegate bool SkyTexDelegate(nint a1, uint a2, float a3, float a4);
	
	private bool SkyTexDetour(nint a1, uint a2, float a3, float a4) {
		if (a2 != 0 && this.Overrides.Advanced)
			a2 = this.Overrides.Props.SkyId;
		return this.SkyTexHook.Original(a1, a2, a3, a4);
	}
}
