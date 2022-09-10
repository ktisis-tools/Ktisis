using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {


		// Cat method will look up for a Category in Category.Categories
		// The name may be ambiguous, but it is used in 200+ locations in this class, so it has shorter name for lisibility
		private static Category Cat(string categoryName)
		{
			if(!Category.Categories.TryGetValue(categoryName, out Category? category))
			{
				Category.Categories.Add(categoryName, new Category(categoryName, new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F)));
				category = Category.Categories[categoryName];
			}
			return category;
		}

		public static Category DefaultCategory => Cat("custom");


		private static readonly Dictionary<string, Category> BonesCategories = new() {
			{ "n_root"          , Cat("body")}, // Root
			{ "n_hara"          , Cat("body")}, // Abdomen
			{ "n_throw"         , Cat("body")}, // Throw
			{ "j_kosi"          , Cat("body")}, // Waist
			{ "j_sebo_a"        , Cat("body")}, // SpineA
			{ "j_asi_a_l"       , Cat("body")}, // LegLeft
			{ "j_asi_a_r"       , Cat("body")}, // LegRight
			{ "j_sebo_b"        , Cat("body")}, // SpineB
			{ "j_asi_b_l"       , Cat("body")}, // KneeLeft
			{ "j_asi_b_r"       , Cat("body")}, // KneeRight
			{ "j_mune_l"        , Cat("body")}, // BreastLeft
			{ "j_mune_r"        , Cat("body")}, // BreastRight
			{ "j_sebo_c"        , Cat("body")}, // SpineC
			{ "j_asi_c_l"       , Cat("body")}, // CalfLeft
			{ "j_asi_c_r"       , Cat("body")}, // CalfRight
			{ "j_kubi"          , Cat("body")}, // Neck
			{ "j_sako_l"        , Cat("body")}, // ClavicleLeft
			{ "j_sako_r"        , Cat("body")}, // ClavicleRight
			{ "j_asi_d_l"       , Cat("body")}, // FootLeft
			{ "j_asi_d_r"       , Cat("body")}, // FootRight
			//{ "j_kao"         , FindCategory("body")}, // Head
			{ "j_ude_a_l"       , Cat("body")}, // ArmLeft
			{ "j_ude_a_r"       , Cat("body")}, // ArmRight
			{ "j_asi_e_l"       , Cat("body")}, // ToesLeft
			{ "j_asi_e_r"       , Cat("body")}, // ToesRight
			{ "j_ude_b_l"       , Cat("body")}, // ForearmLeft
			{ "j_ude_b_r"       , Cat("body")}, // ForearmRight
			{ "n_hkata_l"       , Cat("body")}, // ShoulderLeft
			{ "n_hkata_r"       , Cat("body")}, // ShoulderRight
			{ "j_te_l"          , Cat("body")}, // HandLeft
			{ "j_te_r"          , Cat("body")}, // HandRight
			{ "n_hhiji_l"       , Cat("body")}, // ElbowLeft
			{ "n_hhiji_r"       , Cat("body")}, // ElbowRight

			{ "j_sk_b_b_l"      , Cat("clothes")}, // ClothBackBLeft
			{ "j_sk_b_b_r"      , Cat("clothes")}, // ClothBackBRight
			{ "j_sk_f_b_l"      , Cat("clothes")}, // ClothFrontBLeft
			{ "j_sk_f_b_r"      , Cat("clothes")}, // ClothFrontBRight
			{ "j_sk_s_b_l"      , Cat("clothes")}, // ClothSideBLeft
			{ "j_sk_s_b_r"      , Cat("clothes")}, // ClothSideBRight
			{ "j_buki_sebo_l"   , Cat("clothes")}, // ScabbardLeft
			{ "j_buki_sebo_r"   , Cat("clothes")}, // ScabbardRight
			{ "j_buki2_kosi_l"  , Cat("clothes")}, // HolsterLeft
			{ "j_buki2_kosi_r"  , Cat("clothes")}, // HolsterRight
			{ "j_buki_kosi_l"   , Cat("clothes")}, // SheatheLeft
			{ "j_buki_kosi_r"   , Cat("clothes")}, // SheatheRight
			{ "j_sk_b_a_l"      , Cat("clothes")}, // ClothBackALeft
			{ "j_sk_b_a_r"      , Cat("clothes")}, // ClothBackARight
			{ "j_sk_f_a_l"      , Cat("clothes")}, // ClothFrontALeft
			{ "j_sk_f_a_r"      , Cat("clothes")}, // ClothFrontARight
			{ "j_sk_s_a_l"      , Cat("clothes")}, // ClothSideALeft
			{ "j_sk_s_a_r"      , Cat("clothes")}, // ClothSideARight
			{ "j_sk_b_c_l"      , Cat("clothes")}, // ClothBackCLeft
			{ "j_sk_b_c_r"      , Cat("clothes")}, // ClothBackCRight
			{ "j_sk_f_c_l"      , Cat("clothes")}, // ClothFrontCLeft
			{ "j_sk_f_c_r"      , Cat("clothes")}, // ClothFrontCRight
			{ "j_sk_s_c_l"      , Cat("clothes")}, // ClothSideCLeft
			{ "j_sk_s_c_r"      , Cat("clothes")}, // ClothSideCRight
			{ "n_hizasoubi_l"   , Cat("clothes")}, // PoleynLeft
			{ "n_hizasoubi_r"   , Cat("clothes")}, // PoleynRight
			{ "n_kataarmor_l"   , Cat("clothes")}, // PauldronLeft
			{ "n_kataarmor_r"   , Cat("clothes")}, // PauldronRight
			{ "n_buki_tate_l"   , Cat("clothes")}, // ShieldLeft
			{ "n_buki_tate_r"   , Cat("clothes")}, // ShieldRight
			{ "n_hijisoubi_l"   , Cat("clothes")}, // CouterLeft
			{ "n_hijisoubi_r"   , Cat("clothes")}, // CouterRight
			{ "n_ear_a_l"       , Cat("clothes")}, // EarringALeft
			{ "n_ear_a_r"       , Cat("clothes")}, // EarringARight
			{ "n_ear_b_l"       , Cat("clothes")}, // EarringBLeft
			{ "n_ear_b_r"       , Cat("clothes")}, // EarringBRight

			{ "j_kami_a"        , Cat("hair")}, // HairA
			{ "j_kami_f_l"      , Cat("hair")}, // HairFrontLeft
			{ "j_kami_f_r"      , Cat("hair")}, // HairFrontRight
			{ "j_kami_b"        , Cat("hair")}, // HairB
			{ "j_ex_met_va"     , Cat("hair")}, // HairB

			// hands
			{ "n_hte_r"         , Cat("right hand")}, // WristRight
			{ "j_hito_a_r"      , Cat("right hand")}, // IndexARight
			{ "j_ko_a_r"        , Cat("right hand")}, // PinkyARight
			{ "j_kusu_a_r"      , Cat("right hand")}, // RingARight
			{ "j_naka_a_r"      , Cat("right hand")}, // MiddleARight
			{ "j_oya_a_r"       , Cat("right hand")}, // ThumbARight
			{ "n_buki_r"        , Cat("right hand")}, // WeaponRight
			{ "j_hito_b_r"      , Cat("right hand")}, // IndexBRight
			{ "j_ko_b_r"        , Cat("right hand")}, // PinkyBRight
			{ "j_kusu_b_r"      , Cat("right hand")}, // RingBRight
			{ "j_naka_b_r"      , Cat("right hand")}, // MiddleBRight
			{ "j_oya_b_r"       , Cat("right hand")}, // ThumbBRight


			{ "n_hte_l"         , Cat("left hand")}, // WristLeft
			{ "j_hito_a_l"      , Cat("left hand")}, // IndexALeft
			{ "j_ko_a_l"        , Cat("left hand")}, // PinkyALeft
			{ "j_kusu_a_l"      , Cat("left hand")}, // RingALeft
			{ "j_naka_a_l"      , Cat("left hand")}, // MiddleALeft
			{ "j_oya_a_l"       , Cat("left hand")}, // ThumbALeft
			{ "n_buki_l"        , Cat("left hand")}, // WeaponLeft
			{ "j_hito_b_l"      , Cat("left hand")}, // IndexBLeft
			{ "j_ko_b_l"        , Cat("left hand")}, // PinkyBLeft
			{ "j_kusu_b_l"      , Cat("left hand")}, // RingBLeft
			{ "j_naka_b_l"      , Cat("left hand")}, // MiddleBLeft
			{ "j_oya_b_l"       , Cat("left hand")}, // ThumbBLeft

			// tail
			{ "n_sippo_a"       , Cat("tail")}, // TailA
			{ "n_sippo_b"       , Cat("tail")}, // TailB
			{ "n_sippo_c"       , Cat("tail")}, // TailC
			{ "n_sippo_d"       , Cat("tail")}, // TailD
			{ "n_sippo_e"       , Cat("tail")}, // TailE

			// Head
			{ "j_kao"           , Cat("head")}, // RootHead
			{ "j_ago"           , Cat("head")}, // Jaw
			{ "j_f_dmab_l"      , Cat("head")}, // EyelidLowerLeft
			{ "j_f_dmab_r"      , Cat("head")}, // EyelidLowerRight
			{ "j_f_eye_l"       , Cat("head")}, // EyeLeft
			{ "j_f_eye_r"       , Cat("head")}, // EyeRight
			{ "j_f_hana"        , Cat("head")}, // Nose
			{ "j_f_hoho_l"      , Cat("head")}, // CheekLeft
			{ "j_f_hoho_r"      , Cat("head")}, // CheekRight
			{ "j_f_lip_l"       , Cat("head")}, // LipsLeft
			{ "j_f_lip_r"       , Cat("head")}, // LipsRight
			{ "j_f_mayu_l"      , Cat("head")}, // EyebrowLeft
			{ "j_f_mayu_r"      , Cat("head")}, // EyebrowRight
			{ "j_f_memoto"      , Cat("head")}, // Bridge
			{ "j_f_miken_l"     , Cat("head")}, // BrowLeft
			{ "j_f_miken_r"     , Cat("head")}, // BrowRight
			{ "j_f_ulip_a"      , Cat("head")}, // LipUpperA
			{ "j_f_umab_l"      , Cat("head")}, // EyelidUpperLeft
			{ "j_f_umab_r"      , Cat("head")}, // EyelidUpperRight
			{ "j_f_dlip_a"      , Cat("head")}, // LipLowerA
			{ "j_f_ulip_b"      , Cat("head")}, // LipUpperB
			{ "j_f_dlip_b"      , Cat("head")}, // LipLowerB
			// Hrothgar Faces
			{ "j_f_hige_l"      , Cat("head")}, // HrothWhiskersLeft
			{ "j_f_hige_r"      , Cat("head")}, // HrothWhiskersRight
			//{ "j_f_mayu_l"    , FindCategory("head")}, // HrothEyebrowLeft
			//{ "j_f_mayu_r"    , FindCategory("head")}, // HrothEyebrowRight
			//{ "j_f_memoto"    , FindCategory("head")}, // HrothBridge
			//{ "j_f_miken_l"   , FindCategory("head")}, // HrothBrowLeft
			//{ "j_f_miken_r"   , FindCategory("head")}, // HrothBrowRight
			{ "j_f_uago"        , Cat("head")}, // HrothJawUpper
			{ "j_f_ulip"        , Cat("head")}, // HrothLipUpper
			//{ "j_f_umab_l"    , FindCategory("head")}, // HrothEyelidUpperLeft
			//{ "j_f_umab_r"    , FindCategory("head")}, // HrothEyelidUpperRight
			{ "n_f_lip_l"       , Cat("head")}, // HrothLipsLeft
			{ "n_f_lip_r"       , Cat("head")}, // HrothLipsRight
			{ "n_f_ulip_l"      , Cat("head")}, // HrothLipUpperLeft
			{ "n_f_ulip_r"      , Cat("head")}, // HrothLipUpperRight
			{ "j_f_dlip"        , Cat("head")}, // HrothLipLower


			{ "j_mimi_l"        , Cat("ears")}, // EarLeft
			{ "j_mimi_r"        , Cat("ears")}, // EarRight
			{ "j_zera_a_l"      , Cat("ears")}, // VieraEar01ALeft
			{ "j_zera_a_r"      , Cat("ears")}, // VieraEar01ARight
			{ "j_zera_b_l"      , Cat("ears")}, // VieraEar01BLeft
			{ "j_zera_b_r"      , Cat("ears")}, // VieraEar01BRight
			{ "j_zerb_a_l"      , Cat("ears")}, // VieraEar02ALeft
			{ "j_zerb_a_r"      , Cat("ears")}, // VieraEar02ARight
			{ "j_zerb_b_l"      , Cat("ears")}, // VieraEar02BLeft
			{ "j_zerb_b_r"      , Cat("ears")}, // VieraEar02BRight
			{ "j_zerc_a_l"      , Cat("ears")}, // VieraEar03ALeft
			{ "j_zerc_a_r"      , Cat("ears")}, // VieraEar03ARight
			{ "j_zerc_b_l"      , Cat("ears")}, // VieraEar03BLeft
			{ "j_zerc_b_r"      , Cat("ears")}, // VieraEar03BRight
			{ "j_zerd_a_l"      , Cat("ears")}, // VieraEar04ALeft
			{ "j_zerd_a_r"      , Cat("ears")}, // VieraEar04ARight
			{ "j_zerd_b_l"      , Cat("ears")}, // VieraEar04BLeft
			{ "j_zerd_b_r"      , Cat("ears")}, // VieraEar04BRight
			//{ "j_f_dlip_a"    , FindCategory("head")}, // VieraLipLowerA
			//{ "j_f_ulip_b"    , FindCategory("head")}, // VieraLipUpperB
			//{ "j_f_dlip_b"    , FindCategory("head")}, // VieraLipLowerB

			// IVCS bones
			// 3rd finger joints
			{ "iv_ko_c_l"       , Cat("ivcs left hand")}, // Pinky     rotation
			{ "iv_ko_c_r"       , Cat("ivcs right hand")}, // Pinky
			{ "iv_kusu_c_l"     , Cat("ivcs left hand")}, // Ring
			{ "iv_kusu_c_r"     , Cat("ivcs right hand")}, // Ring
			{ "iv_naka_c_l"     , Cat("ivcs left hand")}, // Middle
			{ "iv_naka_c_r"     , Cat("ivcs right hand")}, // Middle
			{ "iv_hito_c_l"     , Cat("ivcs left hand")}, // Index
			{ "iv_hito_c_r"     , Cat("ivcs right hand")}, // Index
			// Toes
			{ "iv_asi_oya_a_l"  , Cat("ivcs left foot")}, // Big toe   rotation
			{ "iv_asi_oya_b_l"  , Cat("ivcs left foot")}, // Big toe
			{ "iv_asi_oya_a_r"  , Cat("ivcs right foot")}, // Big toe
			{ "iv_asi_oya_b_r"  , Cat("ivcs right foot")}, // Big toe
			{ "iv_asi_hito_a_l" , Cat("ivcs left foot")}, // Index    rotation
			{ "iv_asi_hito_b_l" , Cat("ivcs left foot")}, // Index
			{ "iv_asi_hito_a_r" , Cat("ivcs right foot")}, // Index
			{ "iv_asi_hito_b_r" , Cat("ivcs right foot")}, // Index
			{ "iv_asi_naka_a_l" , Cat("ivcs left foot")}, // Middle    rotation
			{ "iv_asi_naka_b_l" , Cat("ivcs left foot")}, // Middle
			{ "iv_asi_naka_a_r" , Cat("ivcs right foot")}, // Middle
			{ "iv_asi_naka_b_r" , Cat("ivcs right foot")}, // Middle
			{ "iv_asi_kusu_a_l" , Cat("ivcs left foot")}, // Fore toe   rotation
			{ "iv_asi_kusu_b_l" , Cat("ivcs left foot")}, // Fore toe
			{ "iv_asi_kusu_a_r" , Cat("ivcs right foot")}, // Fore toe
			{ "iv_asi_kusu_b_r" , Cat("ivcs right foot")}, // Fore toe
			{ "iv_asi_ko_a_l"   , Cat("ivcs left foot")}, // Pinky toe   rotation
			{ "iv_asi_ko_b_l"   , Cat("ivcs left foot")}, // Pinky toe
			{ "iv_asi_ko_a_r"   , Cat("ivcs right foot")}, // Pinky toe
			{ "iv_asi_ko_b_r"   , Cat("ivcs right foot")}, // Pinky toe
			// Arms
			{ "iv_nitoukin_l"   , Cat("ivcs body")}, // Biceps    rotation, scale, position
			{ "iv_nitoukin_r"   , Cat("ivcs body")}, // Biceps

			// Control override bones (override physics for animations only)
			{ "iv_c_mune_l"     , Cat("ivcs body")}, // Breasts    rotation, scale, position
			{ "iv_c_mune_r"     , Cat("ivcs body")}, // Breasts
			// Genitals
			{ "iv_kougan_l"     , Cat("ivcs penis")}, // Scrotum    rotation, scale, position
			{ "iv_kougan_r"     , Cat("ivcs penis")}, // Scrotum
			{ "iv_ochinko_a"    , Cat("ivcs penis")}, // Penis     rotation, scale*
			{ "iv_ochinko_b"    , Cat("ivcs penis")}, // Penis     *if you want to adjust size
			{ "iv_ochinko_c"    , Cat("ivcs penis")}, // Penis     you will need to adjust
			{ "iv_ochinko_d"    , Cat("ivcs penis")}, // Penis     position and scale
			{ "iv_ochinko_e"    , Cat("ivcs penis")}, // Penis     for all 6 penis bones
			{ "iv_ochinko_f"    , Cat("ivcs penis")}, // Penis     individually
			{ "iv_omanko"       , Cat("ivcs vagina")}, // Vagina    rotation, position, scale
			{ "iv_kuritto"      , Cat("ivcs vagina")}, // Clitoris   rotation, position, scale
			{ "iv_inshin_l"     , Cat("ivcs vagina")}, // Labia    rotation, position, scale
			{ "iv_inshin_r"     , Cat("ivcs vagina")}, // Labia
			// Butt stuffs
			{ "iv_koumon"       , Cat("ivcs buttlocks")}, // Anus    rotation, scale, position
			{ "iv_koumon_l"     , Cat("ivcs buttlocks")}, // Anus
			{ "iv_koumon_r"     , Cat("ivcs buttlocks")}, // Anus
			{ "iv_shiri_l"      , Cat("ivcs buttlocks")}, // Buttocks   rotation, scale, position
			{ "iv_shiri_r"      , Cat("ivcs buttlocks")}, // Buttocks

		};




		public static Category GetCategory(string? boneName)
		{
			if(!BonesCategories.TryGetValue(boneName ?? "", out Category? category))
				category = DefaultCategory;

			if (boneName != null && boneName != "")
				category.RegisterBone(boneName);

			return category;
		}
		public static string GetCategoryName(string? boneName) => GetCategory(boneName).Name;

	}
}
