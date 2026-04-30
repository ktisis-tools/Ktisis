using System.Linq;

using Dalamud.Utility;

using Lumina.Text;

namespace Ktisis.Structs.Extensions {
	public static class SeStringExtensions {
		public static string? FormatName(this string name, sbyte article) {
			if (name.IsNullOrEmpty()) return null;
			
			return article == 1 ? name : string.Join(' ', name.Split(' ').Select((word, index) => {
				if (word.Length <= 1 || index > 0 && word is "of" or "the" or "and")
					return word;
				return word[0].ToString().ToUpper() + word[1..];
			}));
		}
	}
}
