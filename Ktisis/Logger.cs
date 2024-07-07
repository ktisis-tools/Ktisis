using System;

using Dalamud.Logging;

using JetBrains.Annotations;

namespace Ktisis {
	/**
	 * <summary>
	 * Very silly little logger abstraction for the <b>ONE</b> person on this project
	 * who doesn't own the game they're helping mod.
	 * </summary>
	 */
	internal static class Logger {
		/** <summary>If <c>true</c>, this execution is outside of the Dalamud Environment and should not use the Dalamud Logger.</summary> */
		internal static bool IsExternal { get; set; } = false;

		[StringFormatMethod("format")]
		internal static void Fatal(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.DarkRed, format, values);
			else
				Ktisis.Log.Fatal(format, values);
		}

		[StringFormatMethod("format")]
		internal static void Error(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Red, format, values);
			else
				Ktisis.Log.Error(format, values);
		}

		internal static void Error(Exception ex, string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Red, ex, format, values);
			else
				Ktisis.Log.Error(ex, format, values);
		}

		[StringFormatMethod("format")]
		internal static void Warning(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Yellow, format, values);
			else
				Ktisis.Log.Warning(format, values);
		}

		[StringFormatMethod("format")]
		public static void Information(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Cyan, format, values);
			else
				Ktisis.Log.Information(format, values);
		}

		[StringFormatMethod("format")]
		internal static void Verbose(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Gray, format, values);
			else
				Ktisis.Log.Verbose(format, values);
		}

		[StringFormatMethod("format")]
		internal static void Debug(string format, params object[] values) {
			if (IsExternal)
				WriteToConsole(ConsoleColor.Magenta, format, values);
			else
				Ktisis.Log.Debug(format, values);
		}

		[StringFormatMethod("format")]
		private static void WriteToConsole(ConsoleColor color, string format, object[] values) {
			ConsoleColor previousColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(format, values);
			Console.ForegroundColor = previousColor;
		}
		
		[StringFormatMethod("format")]
		private static void WriteToConsole(ConsoleColor color, Exception ex, string format, object[] values) {
			ConsoleColor previousColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(format, values);
			Console.WriteLine(ex);
			Console.ForegroundColor = previousColor;
		}

	}
}
