using Dalamud.IoC;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Ktisis {
	internal class Services {
		[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static ITextureProvider Textures { get; private set; } = null!;
		[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
		[PluginService] internal static IClientState ClientState { get; private set; } = null!;
		[PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static IFramework Framework { get; private set; } = null!;
		[PluginService] internal static IKeyState KeyState { get; private set; } = null!;
		[PluginService] internal static IGameGui GameGui { get; private set; } = null!;
        [PluginService] internal static IGameInteropProvider Hooking { get; private set; } = null!;

		internal static Interop.Hooks.AddonManager AddonManager = null!;
		internal unsafe static TargetSystem* Targets = TargetSystem.Instance();
		internal unsafe static CameraManager* Camera = CameraManager.Instance();

		public static void Init(IDalamudPluginInterface dalamud) {
			dalamud.Create<Services>();
		}
	}
}
