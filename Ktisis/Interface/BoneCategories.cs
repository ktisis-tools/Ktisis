using System.Collections.Generic;

namespace Ktisis.Interface {
	public class BoneCategory {
		public string Name = "";

		public bool IsNsfw = false;

		public List<string> Bones = new();
		public List<BoneCategory> SubCategories = new();

		public BoneCategory? ParentCategory = null;
	}

	public static class BoneCategories {
		public static Dictionary<string, BoneCategory> Categories = new();

		private static Dictionary<string, BoneCategory> BoneCategoryIndex = new();

		private static BoneCategory OTHER_CATEGORY = new() { Name = "Other" };

		public static BoneCategory GetBoneCategory(string bone) {
			if (bone.StartsWith("j_ex_h")) {
				var isHair = bone.Length > 6 && bone[6] >= 48 && bone[6] <= 57; // 5th char is numeric
				if (isHair && Categories.TryGetValue("Hair", out var hair))
					return hair;
			}

			if (BoneCategoryIndex.TryGetValue(bone, out var cat))
				return cat;

			return OTHER_CATEGORY;
		}

		static BoneCategories() {
			// Head -> Face

			var brow = new BoneCategory {
				Name = "Brow",
				Bones = {
					"j_f_miken_l", // BrowLeft
					"j_f_miken_r" // BrowRight
				}
			};

			var eyebrows = new BoneCategory {
				Name = "Eyebrows",
				Bones = {
					"j_f_mayu_l", // EyebrowLeft
					"j_f_mayu_r" // EyebrowRight
				}
			};

			var eyelids = new BoneCategory {
				Name = "Eyelids",
				Bones = {
					"j_f_dmab_l", // EyelidLowerLeft
					"j_f_dmab_r", // EyelidLowerRight
					"j_f_umab_l", // EyelidUpperLeft
					"j_f_umab_r", // EyelidUpperRight
				}
			};

			var eyes = new BoneCategory {
				Name = "Eyes",
				Bones = {
					"j_f_eye_l", // EyeLeft
					"j_f_eye_r" // EyeRight
				},
				SubCategories = {
					eyelids,
					eyebrows
				}
			};

			var upperLip = new BoneCategory {
				Name = "Upper Lip",
				Bones = {
					"j_f_ulip_a", // LipUpperA
					"j_f_ulip_b" // LipUpperB
				}
			};

			var lowerLip = new BoneCategory {
				Name = "Lower Lip",
				Bones = {
					"j_f_dlip_a", // LipLowerA
					"j_f_dlip_b" // LipLowerB
				}
			};

			var mouth = new BoneCategory {
				Name = "Mouth",
				Bones = {
					"j_f_lip_l", // LipsLeft
					"j_f_lip_r" // LipsRight
				},
				SubCategories = {
					upperLip,
					lowerLip
				}
			};

			var cheeks = new BoneCategory {
				Name = "Cheeks",
				Bones = {
					"j_f_hoho_l", // CheekLeft
					"j_f_hoho_r" // CheekRight
				}
			};

			var face = new BoneCategory {
				Name = "Face",
				Bones = {
					"j_f_memoto", // Bridge
					"j_f_hana" // Nose
				},
				SubCategories = {
					brow,
					eyes,
					mouth,
					cheeks
				}
			};

			var ears = new BoneCategory {
				Name = "Ears",
				Bones = {
					"j_mimi_l",     // EarLeft
					"j_mimi_r",     // EarRight
					"j_zera_a_l",   // VieraEar01ALeft
					"j_zera_a_r",   // VieraEar01ARight
					"j_zera_b_l",   // VieraEar01BLeft
					"j_zera_b_r",   // VieraEar01BRight
					"j_zerb_a_l",   // VieraEar02ALeft
					"j_zerb_a_r",   // VieraEar02ARight
					"j_zerb_b_l",   // VieraEar02BLeft
					"j_zerb_b_r",   // VieraEar02BRight
					"j_zerc_a_l",   // VieraEar03ALeft
					"j_zerc_a_r",   // VieraEar03ARight
					"j_zerc_b_l",   // VieraEar03BLeft
					"j_zerc_b_r",   // VieraEar03BRight
					"j_zerd_a_l",   // VieraEar04ALeft
					"j_zerd_a_r",   // VieraEar04ARight
					"j_zerd_b_l",   // VieraEar04BLeft
					"j_zerd_b_r"    // VieraEar04BRight
				}
			};

			var hair = new BoneCategory {
				Name = "Hair",
				Bones = {
					"j_kami_a",    // HairA
					"j_kami_b",    // HairB
					"j_kami_f_l",  // HairFrontLeft
					"j_kami_f_r"   // HairFrontRight
				}
			};

			var head = new BoneCategory {
				Name = "Head",
				Bones = {
					"j_kao", // Head
					"j_ago" // Jaw
				},
				SubCategories = {
					ears,
					hair,
					face
				}
			};

			// Arms -> Hands -> IVCS

			var ivcsHandLeft = new BoneCategory {
				Name = "IVCS Left Hand",
				Bones = {
					"iv_ko_c_l",   // Pinky
					"iv_kusu_c_l", // Ring
					"iv_naka_c_l", // Middle
					"iv_hito_c_l"  // Index
				}
			};

			var ivcsHandRight = new BoneCategory {
				Name = "IVCS Right Hand",
				Bones = {
					"iv_ko_c_r",   // Pinky
					"iv_kusu_c_r", // Ring
					"iv_naka_c_r", // Middle
					"iv_hito_c_r"  // Index
				}
			};

			var handLeft = new BoneCategory {
				Name = "Left Hand",
				Bones = {
					"j_hito_a_l", // IndexALeft
					"j_ko_a_l",   // PinkyALeft
					"j_kusu_a_l", // RingALeft
					"j_naka_a_l", // MiddleALeft
					"j_oya_a_l",  // ThumbALeft
					"j_hito_b_l", // IndexBLeft
					"j_ko_b_l",   // PinkyBLeft
					"j_kusu_b_l", // RingBLeft
					"j_naka_b_l", // MiddleBLeft
					"j_oya_b_l",  // ThumbBLeft
					"j_te_l",     // HandLeft
					"n_hte_l"     // WristLeft
				},
				SubCategories = {
					ivcsHandLeft
				}
			};

			var handRight = new BoneCategory {
				Name = "Right Hand",
				Bones = {
					"j_hito_a_r", // IndexARight
					"j_ko_a_r",   // PinkyARight
					"j_kusu_a_r", // RingARight
					"j_naka_a_r", // MiddleARight
					"j_oya_a_r",  // ThumbARight
					"j_hito_b_r", // IndexBRight
					"j_ko_b_r",   // PinkyBRight
					"j_kusu_b_r", // RingBRight
					"j_naka_b_r", // MiddleBRight
					"j_oya_b_r",  // ThumbBRight
					"j_te_r",     // HandRight
					"n_hte_r"     // WristRight
				},
				SubCategories = {
					ivcsHandRight
				}
			};

			var armLeft = new BoneCategory {
				Name = "Left Arm",
				Bones = {
					"j_sako_l",  // ClavicleLeft
					"j_ude_a_l", // ArmLeft
					"j_ude_b_l", // ForearmLeft
					"n_hkata_l", // ShoulderLeft
					"n_hhiji_l"  // ElbowLeft
				},
				SubCategories = {
					handLeft
				}
			};

			var armRight = new BoneCategory {
				Name = "Right Arm",
				Bones = {
					"j_sako_r",  // ClavicleRight
					"j_ude_a_r", // ArmRight
					"j_ude_b_r", // ForearmRight
					"n_hkata_r", // ShoulderRight
					"n_hhiji_r"  // ElbowRight
				},
				SubCategories = {
					handRight
				}
			};

			var arms = new BoneCategory {
				Name = "Arms",
				SubCategories = {
					armLeft,
					armRight
				}
			};

			// Legs -> Feet -> IVCS

			var ivcsFootLeft = new BoneCategory {
				Name = "IVCS Left Foot",
				Bones = {
					"iv_asi_oya_a_l",  // Big Toe A
					"iv_asi_oya_b_l",  // Big Toe B
					"iv_asi_hito_a_l", // Index A
					"iv_asi_hito_b_l", // Index B
					"iv_asi_naka_a_l", // Middle A
					"iv_asi_naka_b_l", // Middle B
					"iv_asi_kusu_a_l", // Fore Toe A
					"iv_asi_kusu_b_l", // Fore Toe B
					"iv_asi_ko_a_l",   // Pinky Toe A
					"iv_asi_ko_b_l"    // Pinky Toe B
				}
			};

			var ivcsFootRight = new BoneCategory {
				Name = "IVCS Right Foot",
				Bones = {
					"iv_asi_oya_a_r",  // Big Toe A
					"iv_asi_oya_b_r",  // Big Toe B
					"iv_asi_hito_a_r", // Index A
					"iv_asi_hito_b_r", // Index B
					"iv_asi_naka_a_r", // Middle A
					"iv_asi_naka_b_r", // Middle B
					"iv_asi_kusu_a_r", // Fore Toe A
					"iv_asi_kusu_b_r", // Fore Toe B
					"iv_asi_ko_a_r",   // Pinky Toe A
					"iv_asi_ko_b_r"    // Pinky Toe B
				}
			};

			var footLeft = new BoneCategory {
				Name = "Left Foot",
				Bones = {
					"j_asi_d_l", // FootLeft
					"j_asi_e_l"  // ToesLeft
				},
				SubCategories = {
					ivcsFootLeft
				}
			};

			var footRight = new BoneCategory {
				Name = "Right Foot",
				Bones = {
					"j_asi_d_r", // FootRight
					"j_asi_e_r"  // ToesRight
				},
				SubCategories = {
					ivcsFootRight
				}
			};

			var legLeft = new BoneCategory {
				Name = "Left Leg",
				Bones = {
					"j_asi_a_l", // LegLeft
					"j_asi_b_l", // KneeLeft
					"j_asi_c_l"  // CalfLeft
				},
				SubCategories = {
					footLeft
				}
			};

			var legRight = new BoneCategory {
				Name = "Right Leg",
				Bones = {
					"j_asi_a_r", // LegRight
					"j_asi_b_r", // KneeRight
					"j_asi_c_r"  // CalfRight
				},
				SubCategories = {
					footRight
				}
			};

			var legs = new BoneCategory {
				Name = "Legs",
				Bones = {},
				SubCategories = {
					legLeft,
					legRight
				}
			};

			// Tail

			var tail = new BoneCategory {
				Name = "Tail",
				Bones = {
					"n_sippo_a",
					"n_sippo_b",
					"n_sippo_c",
					"n_sippo_d",
					"n_sippo_e"
				}
			};

			// IVCS Penis/Vagina/Ass

			var penis = new BoneCategory {
				Name = "IVCS Penis",
				Bones = {
					// Scrotum
					"iv_kougan_l",
					"iv_kougan_r",
					// Penis
					"iv_ochinko_a",
					"iv_ochinko_b",
					"iv_ochinko_c",
					"iv_ochinko_d",
					"iv_ochinko_e",
					"iv_ochinko_f"
				},
				IsNsfw = true
			};

			var vagina = new BoneCategory {
				Name = "IVCS Vagina",
				Bones = {
					"iv_omanko",
					"iv_kuritto", // Clitoris
					"iv_inshin_l", // Labia Left
					"iv_inshin_r", // Labia Right
				},
				IsNsfw = true
			};

			var ass = new BoneCategory {
				Name = "IVCS Buttocks",
				Bones = {
					// Anus
					"iv_koumon",
					"iv_koumon_l",
					"iv_koumon_r",
					// Buttocks
					"iv_shiri_l",
					"iv_shiri_r"
				},
				IsNsfw = true
			};

			// Breasts

			var ivcsBreasts = new BoneCategory {
				Name = "IVCS Breasts",
				Bones = {
					"iv_c_mune_l",
					"iv_c_mune_r"
				}
			};

			var breasts = new BoneCategory {
				Name = "Breasts",
				Bones = {
					"j_mune_l",  // BreastLeft
					"j_mune_r"   // BreastRight
				},
				SubCategories = {
					ivcsBreasts
				}
			};

			// Body

			var ivcsBody = new BoneCategory {
				Name = "IVCS Body",
				Bones = {
					"iv_nitoukin_l",
					"iv_nitoukin_r"
				}
			};

			Add(
				new BoneCategory {
					Name = "Body",
					Bones = {
						"n_hara",    // Abdomen
						"j_kosi",    // Waist
						"j_sebo_a",  // SpineA
						"j_sebo_b",  // SpineB
						"j_sebo_c",  // SpineC
						"j_kubi"     // Neck
					},
					SubCategories = {
						head,
						arms,
						breasts,
						tail,
						legs,
						ivcsBody,
						penis,
						vagina,
						ass
					}
				}
			);

			// Clothing

			var cloth = new BoneCategory {
				Name = "Cloth",
				Bones = {
					"j_sk_b_b_l", // ClothBackBLeft
					"j_sk_b_b_r", // ClothBackBRight
					"j_sk_f_b_l", // ClothFrontBLeft
					"j_sk_f_b_r", // ClothFrontBRight
					"j_sk_s_b_l", // ClothSideBLeft
					"j_sk_s_b_r", // ClothSideBRight
					"j_sk_b_a_l", // ClothBackALeft
					"j_sk_b_a_r", // ClothBackARight
					"j_sk_f_a_l", // ClothFrontALeft
					"j_sk_f_a_r", // ClothFrontARight
					"j_sk_s_a_l", // ClothSideALeft
					"j_sk_s_a_r", // ClothSideARight
					"j_sk_b_c_l", // ClothBackCLeft
					"j_sk_b_c_r", // ClothBackCRight
					"j_sk_f_c_l", // ClothFrontCLeft
					"j_sk_f_c_r", // ClothFrontCRight
					"j_sk_s_c_l", // ClothSideCLeft
					"j_sk_s_c_r"  // ClothSideCRight
				}
			};

			var earring = new BoneCategory {
				Name = "Earring",
				Bones = {
					"n_ear_a_l", // EarringALeft
					"n_ear_a_r", // EarringARight
					"n_ear_b_l", // EarringBLeft
					"n_ear_b_r"  // EarringBRight
				}
			};

			Add(
				new BoneCategory {
					Name = "Clothing",
					Bones = {
						"n_hizasoubi_l",  // PoleynLeft
						"n_hizasoubi_r",  // PoleynRight
						"n_kataarmor_l",  // PauldronLeft
						"n_kataarmor_r",  // PauldronRight
						"n_hijisoubi_l",  // CouterLeft
						"n_hijisoubi_r",  // CouterRight
						"j_ex_top_a_r",
						"j_ex_top_a_l",
						"j_ex_top_b_r",
						"j_ex_top_b_l",
						"j_ex_met_a",
						"j_ex_met_b",
						"j_ex_met_c",
						"j_ex_met_d",
						"j_ex_met_va", // VisorA
						"j_ex_met_vb"  // VisorB
					},
					SubCategories = {
						cloth,
						earring
					}
				}	
			);

			// Weapons

			Add(
				new BoneCategory {
					Name = "Weapons",
					Bones = {
						"j_buki_sebo_l",  // ScabbardLeft
						"j_buki_sebo_r",  // ScabbardRight
						"j_buki2_kosi_l", // HolsterLeft
						"j_buki2_kosi_r", // HolsterRight
						"j_buki_kosi_l",  // SheatheLeft
						"j_buki_kosi_r",  // SheatheRight
						"n_buki_r",       // WeaponRight
						"n_buki_l",       // WeaponLeft
						"n_buki_tate_l",  // ShieldLeft
						"n_buki_tate_r"   // ShieldRight
					}
				}
			);

			// TODO

			Add(OTHER_CATEGORY);
		}

		private static void Add(BoneCategory category) {
			Categories.Add(category.Name, category);

			foreach (var bone in category.Bones) {
				BoneCategoryIndex.Add(bone, category);
			}

			foreach (var child in category.SubCategories) {
				child.ParentCategory = category;
				Add(child);
			}
		}
	}
}