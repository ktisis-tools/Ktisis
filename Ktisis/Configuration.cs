using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuizmoNET;

using Dalamud;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Interface;
using Ktisis.Legacy.Config;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip.SetSources;
using static Ktisis.Data.Files.AnamCharaFile;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public const int CurVersion = 3;
		public int Version { get; set; } = CurVersion;

		public bool IsFirstTimeInstall = true;
		public string LastPluginVer = "";

		// Ktisis

		public OpenKtisisMethod OpenKtisisMethod = OpenKtisisMethod.OnEnterGpose;

		// Overlay

		public bool ShowOverlay = true;

		public bool DrawLinesOnSkeleton = true;
		public bool DrawLinesWithGizmo = true;
		public bool DrawDotsWithGizmo = true;

		public float SkeletonDotRadius = 3.0f;
		public float SkeletonLineOpacity = 0.95f;
		public float SkeletonLineThickness = 2.0f;
		public float SkeletonLineOpacityWhileUsing = 0.15f;

		// Gizmo

		public MODE GizmoMode = MODE.LOCAL;
		public OPERATION GizmoOp = OPERATION.ROTATE;

		public SiblingLink SiblingLink = SiblingLink.None;

		public bool AllowAxisFlip = true;

		// Targeting

		public bool AllowTargetOnLeftClick = true;
		public bool AllowTargetOnRightClick = true;

		// UI

		public bool CensorNsfw = false;
		public bool DisplayCharName = true;

		public bool EnableParenting = true;

		// Language

		public bool TranslateBones = true;

		public UserLocale Language = UserLocale.English;

		// Keybinds

		public bool EnableKeybinds = false;

		// Data

		public Dictionary<string, Dictionary<string, Vector3>> CustomBoneOffset = new();
		public Dictionary<string, GlamourDresser.GlamourPlate[]?>? GlamourPlateData = null;

		// Get config

		public static Configuration GetConfig(IPluginConfiguration? cfg) {
			if (cfg == null)
				return new();

			var version = cfg.Version;
			if (version < 3) {
				Logger.Information($"Starting config migration from version {version}.");
				if (cfg is ConfigV2 v2) {
					return v2.Migrate();
				} else {
					Logger.Warning("Migration failed. Config has been reset.");
					return new();
				}
			}

			return cfg as Configuration ?? new();
		}
	}

	[Serializable]
	public class ReferenceInfo {
		public bool Showing { get; set; }
		public string? Path { get; set; }
	}

	[Serializable]
	public enum OpenKtisisMethod {
		Manually,
		OnPluginLoad,
		OnEnterGpose,
	}
}
