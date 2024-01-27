using System;

namespace Ktisis.Common.Utility;

public static class PlayerNameUtil {
	// This is required for Penumbra collection assignments.
	
	private readonly static string[] Single = [ "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen" ];
	private readonly static string[] Tens = [ "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" ];

	public static string CalcActorName(ushort index, string firstName = "Actor") => $"{firstName} {CalcActorNameWords(index)}";

	private static string CalcActorNameWords(ushort index) {
		if (index >= 200) index -= 200;

		if (index < Single.Length)
			return Single[index];

		if (index < 20) {
			var baseNum = index - 10;
			var baseWord = Single[baseNum];
			if (baseWord.EndsWith('t'))
				baseWord = baseWord[..^1];
			return baseWord + "teen";
		}

		var ten = (int)Math.Floor((decimal)(index - 20) / 10);
		var unit = index % 10;

		var suffix = Tens[ten];
		if (unit == 0) return suffix;
		
		return suffix + "-" + Single[unit].ToLowerInvariant();
	}
}
