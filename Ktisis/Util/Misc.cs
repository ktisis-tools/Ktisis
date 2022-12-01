using System;
using System.IO.Compression;
using System.IO;
using System.Text;

using ImGuiNET;
using Newtonsoft.Json;

namespace Ktisis.Util {
	internal class Misc {


		public static void ExportClipboard(object? objectToExport) {
			var str = JsonConvert.SerializeObject(objectToExport);
			if (!Ktisis.Configuration.ClipboardExportClearJson)
				str = Convert.ToBase64String(CompressString(str));
			ImGui.SetClipboardText(str);
		}
		public static T? ImportClipboard<T>() {
			var rawString = ImGui.GetClipboardText();
			if (rawString == null) return default;

			if (IsBase64String(rawString)) {
				var base64String = Convert.FromBase64String(rawString);

				try {
					var compressedMaybe = JsonConvert.DeserializeObject<T>(DecompressString(base64String));
					if (compressedMaybe != null)
						return compressedMaybe;
				} catch (Exception) { }
				try {
					var base64Maybe = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(base64String));
					if (base64Maybe != null)
						return base64Maybe;
				} catch (Exception) { }
			}

			try {
				return JsonConvert.DeserializeObject<T>(rawString);
			} catch (JsonReaderException) {
				return default;
			}
		}

		public static bool IsBase64String(string base64) {
			Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
			return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
		}

		public static byte[] CompressString(string str) {
			var bytes = Encoding.UTF8.GetBytes(str);

			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream()) {
				using (var gs = new GZipStream(mso, CompressionMode.Compress))
					msi.CopyTo(gs);

				return mso.ToArray();
			}
		}

		public static string DecompressString(byte[] bytes) {
			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream()) {
				using (var gs = new GZipStream(msi, CompressionMode.Decompress))
					gs.CopyTo(mso);

				return Encoding.UTF8.GetString(mso.ToArray());
			}
		}
	}
}
