using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {


		public static Category FindCategory(string categoryName)
		{
			Category.Categories.TryGetValue(categoryName, out Category? category);
			category ??= DefaultCategory; // TODO: potential infinite loop
			return category;
		}

		public static Category DefaultCategory => FindCategory("body");


		private static readonly Dictionary<string, Category> BonesCategories = new() {
			{ "n_root"        , FindCategory("body")}, // Root
			{ "n_hara"        , FindCategory("body")}, // Abdomen
			{ "n_throw"       , FindCategory("body")}, // Throw
			{ "j_kosi"        , FindCategory("body")}, // Waist
			{ "j_sebo_a"      , FindCategory("body")}, // SpineA
			{ "j_asi_a_l"     , FindCategory("body")}, // LegLeft
			{ "j_asi_a_r"     , FindCategory("body")}, // LegRight
			{ "j_buki2_kosi_l", FindCategory("clothes")}, // HolsterLeft
			{ "j_buki2_kosi_r", FindCategory("clothes")}, // HolsterRight
			{ "j_buki_kosi_l" , FindCategory("clothes")}, // SheatheLeft
			{ "j_buki_kosi_r" , FindCategory("clothes")}, // SheatheRight
			{ "j_sebo_b"      , FindCategory("body")}, // SpineB
			{ "j_sk_b_a_l"    , FindCategory("clothes")}, // ClothBackALeft
			{ "j_sk_b_a_r"    , FindCategory("clothes")}, // ClothBackARight
			{ "j_sk_f_a_l"    , FindCategory("clothes")}, // ClothFrontALeft
			{ "j_sk_f_a_r"    , FindCategory("clothes")}, // ClothFrontARight
			{ "j_sk_s_a_l"    , FindCategory("clothes")}, // ClothSideALeft
			{ "j_sk_s_a_r"    , FindCategory("clothes")}, // ClothSideARight
			{ "j_asi_b_l"     , FindCategory("body")}, // KneeLeft
			{ "j_asi_b_r"     , FindCategory("body")}, // KneeRight
			{ "j_mune_l"      , FindCategory("body")}, // BreastLeft
			{ "j_mune_r"      , FindCategory("body")}, // BreastRight
			{ "j_sebo_c"      , FindCategory("body")}, // SpineC
			{ "j_sk_b_b_l"    , FindCategory("clothes")}, // ClothBackBLeft
			{ "j_sk_b_b_r"    , FindCategory("clothes")}, // ClothBackBRight
			{ "j_sk_f_b_l"    , FindCategory("clothes")}, // ClothFrontBLeft
			{ "j_sk_f_b_r"    , FindCategory("clothes")}, // ClothFrontBRight
			{ "j_sk_s_b_l"    , FindCategory("clothes")}, // ClothSideBLeft
			{ "j_sk_s_b_r"    , FindCategory("clothes")}, // ClothSideBRight
			{ "j_asi_c_l"     , FindCategory("body")}, // CalfLeft
			{ "j_asi_c_r"     , FindCategory("body")}, // CalfRight
			{ "j_buki_sebo_l" , FindCategory("clothes")}, // ScabbardLeft
			{ "j_buki_sebo_r" , FindCategory("clothes")}, // ScabbardRight
			{ "j_kubi"        , FindCategory("body")}, // Neck
			{ "j_sako_l"      , FindCategory("body")}, // ClavicleLeft
			{ "j_sako_r"      , FindCategory("body")}, // ClavicleRight
			{ "j_sk_b_c_l"    , FindCategory("clothes")}, // ClothBackCLeft
			{ "j_sk_b_c_r"    , FindCategory("clothes")}, // ClothBackCRight
			{ "j_sk_f_c_l"    , FindCategory("clothes")}, // ClothFrontCLeft
			{ "j_sk_f_c_r"    , FindCategory("clothes")}, // ClothFrontCRight
			{ "j_sk_s_c_l"    , FindCategory("clothes")}, // ClothSideCLeft
			{ "j_sk_s_c_r"    , FindCategory("clothes")}, // ClothSideCRight
			{ "n_hizasoubi_l" , FindCategory("clothes")}, // PoleynLeft
			{ "n_hizasoubi_r" , FindCategory("clothes")}, // PoleynRight
			{ "j_asi_d_l"     , FindCategory("body")}, // FootLeft
			{ "j_asi_d_r"     , FindCategory("body")}, // FootRight
			//{ "j_kao"         , FindCategory("body")}, // Head
			{ "j_ude_a_l"     , FindCategory("body")}, // ArmLeft
			{ "j_ude_a_r"     , FindCategory("body")}, // ArmRight
			{ "n_kataarmor_l" , FindCategory("clothes")}, // PauldronLeft
			{ "n_kataarmor_r" , FindCategory("clothes")}, // PauldronRight
			{ "j_asi_e_l"     , FindCategory("body")}, // ToesLeft
			{ "j_asi_e_r"     , FindCategory("body")}, // ToesRight
			{ "j_kami_a"      , FindCategory("hair")}, // HairA
			{ "j_kami_f_l"    , FindCategory("hair")}, // HairFrontLeft
			{ "j_kami_f_r"    , FindCategory("hair")}, // HairFrontRight
			{ "j_mimi_l"      , FindCategory("head")}, // EarLeft
			{ "j_mimi_r"      , FindCategory("head")}, // EarRight
			{ "j_ude_b_l"     , FindCategory("body")}, // ForearmLeft
			{ "j_ude_b_r"     , FindCategory("body")}, // ForearmRight
			{ "n_hkata_l"     , FindCategory("body")}, // ShoulderLeft
			{ "n_hkata_r"     , FindCategory("body")}, // ShoulderRight
			{ "j_kami_b"      , FindCategory("hair")}, // HairB
			{ "j_ex_met_va"   , FindCategory("hair")}, // HairB
			{ "j_te_l"        , FindCategory("body")}, // HandLeft
			{ "j_te_r"        , FindCategory("body")}, // HandRight
			{ "n_buki_tate_l" , FindCategory("clothes")}, // ShieldLeft
			{ "n_buki_tate_r" , FindCategory("clothes")}, // ShieldRight
			{ "n_ear_a_l"     , FindCategory("head")}, // EarringALeft
			{ "n_ear_a_r"     , FindCategory("head")}, // EarringARight
			{ "n_hhiji_l"     , FindCategory("body")}, // ElbowLeft
			{ "n_hhiji_r"     , FindCategory("body")}, // ElbowRight
			{ "n_hijisoubi_l" , FindCategory("clothes")}, // CouterLeft
			{ "n_hijisoubi_r" , FindCategory("clothes")}, // CouterRight

			// hands
			{ "n_hte_l"       , FindCategory("left hand")}, // WristLeft
			{ "n_hte_r"       , FindCategory("right hand")}, // WristRight
			{ "j_hito_a_l"    , FindCategory("left hand")}, // IndexALeft
			{ "j_hito_a_r"    , FindCategory("right hand")}, // IndexARight
			{ "j_ko_a_l"      , FindCategory("left hand")}, // PinkyALeft
			{ "j_ko_a_r"      , FindCategory("right hand")}, // PinkyARight
			{ "j_kusu_a_l"    , FindCategory("left hand")}, // RingALeft
			{ "j_kusu_a_r"    , FindCategory("right hand")}, // RingARight
			{ "j_naka_a_l"    , FindCategory("left hand")}, // MiddleALeft
			{ "j_naka_a_r"    , FindCategory("right hand")}, // MiddleARight
			{ "j_oya_a_l"     , FindCategory("left hand")}, // ThumbALeft
			{ "j_oya_a_r"     , FindCategory("right hand")}, // ThumbARight
			{ "n_buki_l"      , FindCategory("left hand")}, // WeaponLeft
			{ "n_buki_r"      , FindCategory("right hand")}, // WeaponRight
			{ "n_ear_b_l"     , FindCategory("head")}, // EarringBLeft
			{ "n_ear_b_r"     , FindCategory("head")}, // EarringBRight
			{ "j_hito_b_l"    , FindCategory("left hand")}, // IndexBLeft
			{ "j_hito_b_r"    , FindCategory("right hand")}, // IndexBRight
			{ "j_ko_b_l"      , FindCategory("left hand")}, // PinkyBLeft
			{ "j_ko_b_r"      , FindCategory("right hand")}, // PinkyBRight
			{ "j_kusu_b_l"    , FindCategory("left hand")}, // RingBLeft
			{ "j_kusu_b_r"    , FindCategory("right hand")}, // RingBRight
			{ "j_naka_b_l"    , FindCategory("left hand")}, // MiddleBLeft
			{ "j_naka_b_r"    , FindCategory("right hand")}, // MiddleBRight
			{ "j_oya_b_l"     , FindCategory("left hand")}, // ThumbBLeft
			{ "j_oya_b_r"     , FindCategory("right hand")}, // ThumbBRight

			// tail
			{ "n_sippo_a"     , FindCategory("tail")}, // TailA
			{ "n_sippo_b"     , FindCategory("tail")}, // TailB
			{ "n_sippo_c"     , FindCategory("tail")}, // TailC
			{ "n_sippo_d"     , FindCategory("tail")}, // TailD
			{ "n_sippo_e"     , FindCategory("tail")}, // TailE

			// Head
			{ "j_kao"         , FindCategory("head")}, // RootHead
			{ "j_ago"         , FindCategory("head")}, // Jaw
			{ "j_f_dmab_l"    , FindCategory("head")}, // EyelidLowerLeft
			{ "j_f_dmab_r"    , FindCategory("head")}, // EyelidLowerRight
			{ "j_f_eye_l"     , FindCategory("head")}, // EyeLeft
			{ "j_f_eye_r"     , FindCategory("head")}, // EyeRight
			{ "j_f_hana"      , FindCategory("head")}, // Nose
			{ "j_f_hoho_l"    , FindCategory("head")}, // CheekLeft
			{ "j_f_hoho_r"    , FindCategory("head")}, // CheekRight
			{ "j_f_lip_l"     , FindCategory("head")}, // LipsLeft
			{ "j_f_lip_r"     , FindCategory("head")}, // LipsRight
			{ "j_f_mayu_l"    , FindCategory("head")}, // EyebrowLeft
			{ "j_f_mayu_r"    , FindCategory("head")}, // EyebrowRight
			{ "j_f_memoto"    , FindCategory("head")}, // Bridge
			{ "j_f_miken_l"   , FindCategory("head")}, // BrowLeft
			{ "j_f_miken_r"   , FindCategory("head")}, // BrowRight
			{ "j_f_ulip_a"    , FindCategory("head")}, // LipUpperA
			{ "j_f_umab_l"    , FindCategory("head")}, // EyelidUpperLeft
			{ "j_f_umab_r"    , FindCategory("head")}, // EyelidUpperRight
			{ "j_f_dlip_a"    , FindCategory("head")}, // LipLowerA
			{ "j_f_ulip_b"    , FindCategory("head")}, // LipUpperB
			{ "j_f_dlip_b"    , FindCategory("head")}, // LipLowerB

			// Viera Ears
			{ "j_zera_a_l"    , FindCategory("ears")}, // VieraEar01ALeft
			{ "j_zera_a_r"    , FindCategory("ears")}, // VieraEar01ARight
			{ "j_zera_b_l"    , FindCategory("ears")}, // VieraEar01BLeft
			{ "j_zera_b_r"    , FindCategory("ears")}, // VieraEar01BRight
			{ "j_zerb_a_l"    , FindCategory("ears")}, // VieraEar02ALeft
			{ "j_zerb_a_r"    , FindCategory("ears")}, // VieraEar02ARight
			{ "j_zerb_b_l"    , FindCategory("ears")}, // VieraEar02BLeft
			{ "j_zerb_b_r"    , FindCategory("ears")}, // VieraEar02BRight
			{ "j_zerc_a_l"    , FindCategory("ears")}, // VieraEar03ALeft
			{ "j_zerc_a_r"    , FindCategory("ears")}, // VieraEar03ARight
			{ "j_zerc_b_l"    , FindCategory("ears")}, // VieraEar03BLeft
			{ "j_zerc_b_r"    , FindCategory("ears")}, // VieraEar03BRight
			{ "j_zerd_a_l"    , FindCategory("ears")}, // VieraEar04ALeft
			{ "j_zerd_a_r"    , FindCategory("ears")}, // VieraEar04ARight
			{ "j_zerd_b_l"    , FindCategory("ears")}, // VieraEar04BLeft
			{ "j_zerd_b_r"    , FindCategory("ears")}, // VieraEar04BRight
			//{ "j_f_dlip_a"    , FindCategory("head")}, // VieraLipLowerA
			//{ "j_f_ulip_b"    , FindCategory("head")}, // VieraLipUpperB
			//{ "j_f_dlip_b"    , FindCategory("head")}, // VieraLipLowerB

			// Hrothgar Faces
			{ "j_f_hige_l"    , FindCategory("head")}, // HrothWhiskersLeft
			{ "j_f_hige_r"    , FindCategory("head")}, // HrothWhiskersRight
			//{ "j_f_mayu_l"    , FindCategory("head")}, // HrothEyebrowLeft
			//{ "j_f_mayu_r"    , FindCategory("head")}, // HrothEyebrowRight
			//{ "j_f_memoto"    , FindCategory("head")}, // HrothBridge
			//{ "j_f_miken_l"   , FindCategory("head")}, // HrothBrowLeft
			//{ "j_f_miken_r"   , FindCategory("head")}, // HrothBrowRight
			{ "j_f_uago"      , FindCategory("head")}, // HrothJawUpper
			{ "j_f_ulip"      , FindCategory("head")}, // HrothLipUpper
			//{ "j_f_umab_l"    , FindCategory("head")}, // HrothEyelidUpperLeft
			//{ "j_f_umab_r"    , FindCategory("head")}, // HrothEyelidUpperRight
			{ "n_f_lip_l"     , FindCategory("head")}, // HrothLipsLeft
			{ "n_f_lip_r"     , FindCategory("head")}, // HrothLipsRight
			{ "n_f_ulip_l"    , FindCategory("head")}, // HrothLipUpperLeft
			{ "n_f_ulip_r"    , FindCategory("head")}, // HrothLipUpperRight
			{ "j_f_dlip"      , FindCategory("head")}, // HrothLipLower
		};




		public static Category GetCategory(string? boneName)
		{
			boneName ??= "";
			BonesCategories.TryGetValue(boneName, out Category? cat);
			cat ??= DefaultCategory;
			return cat;
		}
		public static string GetCategoryName(string? boneName) => GetCategory(boneName).Name;

	}
}
