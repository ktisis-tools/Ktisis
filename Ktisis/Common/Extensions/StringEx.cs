using System;
using System.Linq;

using Dalamud.Utility;
using Dalamud.Bindings.ImGui;

namespace Ktisis.Common.Extensions;

public static class StringEx {
	public static string Truncate(this string str, int len, bool ellipsis = true) {
		if (str.Length <= len) return str;
		var newLen = Math.Min(len, str.Length);
		var dots = Math.Min(len - 2, 3);
		if (dots <= 1 || !ellipsis) return str[..newLen];
		newLen -= dots;
		return str[..newLen] + new string('.', dots);
	}

	public static string FitToWidth(this string str, float width, bool ellipsis = true) {
		var result = str;
		var length = result.Length;

		var isTrunc = false;
		while (length > 0 && ImGui.CalcTextSize(result).X > width) {
			isTrunc = true;
			length--;
			result = result[..length];
		}

		if (ellipsis && isTrunc && length >= 5) {
			length -= 3;
			result = result[..length] + new string('.', 3);
		}
		
		return result;
	}
	
	public static string? FormatName(this string name, sbyte article) {
		if (name.IsNullOrEmpty()) return null;
		
		return article == 1 ? name : string.Join(' ', name.Split(' ').Select((word, index) => {
			if (word.Length <= 1 || index > 0 && word is "of" or "the" or "and")
				return word;
			return word[0].ToString().ToUpper() + word[1..];
		}));
	}
}
