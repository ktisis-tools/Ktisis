using System.IO;
using System.Numerics;
using System.Reflection;
using System.Diagnostics;

using ImGuiNET;

namespace Ktisis.Interface.Library {
	internal static class Common {
		internal static Vector4 ColRed = new Vector4(255, 0, 0, 255);
		internal static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		internal static Vector4 ColBlue = new Vector4(0, 0, 255, 255);
		internal static Vector4 ColYellow = new Vector4(255, 250, 0, 255);

		// From SimpleTweaks - Thanks Caraxi
		internal static void OpenBrowser(string url)
			=> Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

		// Open assembly file
		internal static Stream GetAssemblyFile(string path) {
			var assembly = Assembly.GetExecutingAssembly();
			string assemblyName = assembly.GetName().Name!;

			path = $"{assemblyName}.{path}";

			Stream? stream = assembly.GetManifestResourceStream(path);
			if (stream == null)
				throw new FileNotFoundException(path);

			return stream;
		}

		// Item tooltip
		internal static void Tooltip(string text) {
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}
	}
}
