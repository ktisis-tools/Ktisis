using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

using ImGuiNET;

namespace Ktisis.Interface.Windows {
	internal static class References {
		/** Maps file paths to loaded textures. */
		public static Dictionary<string, ISharedImmediateTexture> Textures = new();

		// Draw

		public static void Draw() {
			var cfg = Ktisis.Configuration;
			foreach (var (key, reference) in cfg.References) {
				DrawReferenceWindow(cfg, key, reference);
			}
		}

		public static void DrawReferenceWindow(Configuration cfg, int key, ReferenceInfo reference) {
			if (!reference.Showing) return;
			if (reference.Path is not string path) return;
			if (!Textures.TryGetValue(path, out var sharedTex))
				return;

			var texture = sharedTex.GetWrapOrEmpty();

			var size = new Vector2(texture.Width, texture.Height);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(50, 50), size);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);

			var flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar;
			if (cfg.ReferenceHideDecoration) {
				flags |= ImGuiWindowFlags.NoDecoration;
			}
			if (ImGui.Begin($"{reference.Path}##reference{key}", flags)) {
				var scaled = new Vector2(ImGui.GetWindowWidth(), texture.Height * ImGui.GetWindowWidth() / texture.Width );
				var tintColor = new Vector4(1f, 1f, 1f, cfg.ReferenceAlpha);
				ImGui.Image(texture.ImGuiHandle, scaled, Vector2.Zero, Vector2.One, tintColor);
			}
			ImGui.PopStyleVar(2);
			ImGui.End();
		}

		public static bool LoadReferences(Configuration cfg) {
			return cfg.References.Select(LoadReference).All(x => x);
		}

		public static bool LoadReference(KeyValuePair<int, ReferenceInfo> reference) {
			var path = reference.Value.Path;
			try {
				if (path == null) return false;
				Textures[path] = Services.Textures.GetFromFile(path);
				return true;
			} catch (Exception e) {
				Logger.Error(e, "Failed to load reference image {0}", path ?? "null");
				return false;
			}
		}

		public static void DisposeUnreferencedTextures(Configuration cfg) {
			var loadedPaths = Textures.Keys;
			var paths = cfg.References.Values.Select(x => x.Path ?? "");
			var unloadPaths = loadedPaths.Except(paths);
			foreach (var path in unloadPaths) {
				Textures.Remove(path);
			}
		}
	}
}
