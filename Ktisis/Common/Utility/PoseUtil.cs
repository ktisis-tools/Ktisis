using System.Linq;

namespace Ktisis.Common.Utility;

public static class PoseUtil {
	public readonly static string[] BunnyEarBones = {
		"j_zera_a_l", "j_zera_b_l",
		"j_zera_a_r", "j_zera_b_r",
		"j_zerb_a_l", "j_zerb_b_l",
		"j_zerb_a_r", "j_zerb_b_r",
		"j_zerc_a_l", "j_zerc_b_l",
		"j_zerc_a_r", "j_zerc_b_r",
		"j_zerd_a_l", "j_zerd_b_l",
		"j_zerd_a_r", "j_zerd_b_r"
	};

	public static string[] EarBones { get => field.Concat(BunnyEarBones).ToArray(); } = {
		"j_mimi_l", "j_mimi_r",
		"n_ear_a_l", "n_ear_a_r",
		"n_ear_b_l", "n_ear_b_r",
	};
}
