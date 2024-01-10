using System;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.Framework;

using Ktisis.Interop.Hooking;
using Ktisis.Structs.Env;

namespace Ktisis.Scene.Modules;

[Flags]
public enum EnvOverride {
	None = 0x000,
	TimeWeather = 0x001,
	SkyId = 0x002,
	Lighting = 0x04,
	Stars = 0x008,
	Fog = 0x010,
	Clouds = 0x020,
	Rain = 0x040,
	Dust = 0x080,
	Wind = 0x100
}

public interface IEnvModule : IHookModule {
	public EnvOverride Override { get; set; }
	
	public float Time { get; set; }
	public int Day { get; set; }
	public byte Weather { get; set; }
}

public class EnvModule : SceneModule, IEnvModule {
	private readonly IFramework _framework;
	
	public EnvModule(
		IHookMediator hook,
		ISceneManager scene,
		IFramework framework
	) : base(hook, scene) {
		this._framework = framework;
	}

	public override bool Initialize() {
		var result = base.Initialize();
		if (result) this.EnableAll();
		return result;
	}
	
	// EnvState

	public EnvOverride Override { get; set; } = EnvOverride.None;

	public float Time { get; set; }
	public int Day { get; set; }

	public byte Weather { get; set; }
	public float MoonPhase { get; set; }
	
	private unsafe void ApplyState(EnvState* dest, EnvState state) {
		var flags = Enum.GetValues<EnvOverride>()
			.Where(flag => flag > EnvOverride.TimeWeather && this.Override.HasFlag(flag));

		foreach (var flag in flags) {
			switch (flag) {
				case EnvOverride.SkyId:
					dest->SkyId = state.SkyId;
					break;
				case EnvOverride.Lighting:
					dest->Lighting = state.Lighting;
					break;
				case EnvOverride.Stars:
					dest->Stars = state.Stars;
					break;
				case EnvOverride.Fog:
					dest->Fog = state.Fog;
					break;
				case EnvOverride.Clouds:
					dest->Clouds = state.Clouds;
					break;
				case EnvOverride.Rain:
					dest->Rain = state.Rain;
					break;
				case EnvOverride.Dust:
					dest->Dust = state.Dust;
					break;
				case EnvOverride.Wind:
					dest->Wind = state.Wind;
					break;
			}
		}
	}
	
	// Hooks
	
	private unsafe delegate nint EnvStateCopyDelegate(EnvState* dest, EnvState* src);
	private unsafe delegate nint EnvManagerUpdateDelegate(EnvManagerEx* env, float a2, float a3);
	private delegate void UpdateTimeDelegate(nint a1);

	[Signature("E8 ?? ?? ?? ?? 49 3B F5")]
	private EnvStateCopyDelegate EnvStateCopy = null!;

	[Signature("E8 ?? ?? ?? ?? 49 3B F5", DetourName = nameof(EnvStateCopyDetour))]
	private Hook<EnvStateCopyDelegate> EnvStateCopyHook = null!;
	private unsafe nint EnvStateCopyDetour(EnvState* dest, EnvState* src) {
		EnvState? original = null;
		if (this.Scene.IsValid && this.Override != 0)
			original = *dest;
		var exec = this.EnvStateCopyHook.Original(dest, src);
		if (original != null)
			this.ApplyState(dest, original.Value);
		return exec;
	}

	[Signature("E8 ?? ?? ?? ?? 49 8B 0E 48 8D 93 ?? ?? ?? ??", DetourName = nameof(EnvUpdateDetour))]
	private Hook<EnvManagerUpdateDelegate> EnvUpdateHook = null!;
	private unsafe nint EnvUpdateDetour(EnvManagerEx* env, float a2, float a3) {
		if (this.Scene.IsValid && this.Override.HasFlag(EnvOverride.TimeWeather)) {
			env->_base.DayTimeSeconds = this.Time;
			env->_base.ActiveWeather = this.Weather;
		}
		return this.EnvUpdateHook.Original(env, a2, a3);
	}

	[Signature("48 89 5C 24 ?? 57 48 83 EC 30 4C 8B 15 ?? ?? ?? ??", DetourName = nameof(UpdateTimeDetour))]
	private Hook<UpdateTimeDelegate> UpdateTimeHook = null!;
	private unsafe void UpdateTimeDetour(nint a1) {
		if (this.Scene.IsValid && this.Override.HasFlag(EnvOverride.TimeWeather)) {
			var currentTime = (long)(this.Day * 86400 + this.Time);
			var clientTime = &Framework.Instance()->ClientTime;
			clientTime->EorzeaTime = currentTime;
			clientTime->EorzeaTimeOverride = currentTime;
		}
		this.UpdateTimeHook.Original(a1);
	}
}
