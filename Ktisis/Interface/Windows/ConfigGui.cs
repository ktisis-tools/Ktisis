using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using ImGuiNET;

using Newtonsoft.Json;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;

namespace Ktisis.Interface.Windows {
	internal static class ConfigGui {
		public static bool Visible = false;
		public static Vector2 ButtonSize = new Vector2(ImGui.GetFontSize() * 1.50f);
		private static Vector2 WindowSizeMin = new(ImGui.GetFontSize() * 15, ImGui.GetFontSize() * 20);
		private static Vector2 WindowSizeMax = ImGui.GetIO().DisplaySize * 0.85f;

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}
		public static void Toggle() => Visible = !Visible;

		// Draw

		public static void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(WindowSizeMin, WindowSizeMax);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin(Locale.GetString("config.title"), ref Visible)) {
				if (ImGui.BeginTabBar(Locale.GetString("config.title"))) {
					var cfg = Ktisis.Configuration;
					if (ImGui.BeginTabItem(Locale.GetString("config.interface.title")))
						DrawInterfaceTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.overlay.title")))
						DrawOverlayTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.gizmo.title")))
						DrawGizmoTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.input.title")))
						DrawInputTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.references.title")))
						DrawReferencesTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.language.title")))
						DrawLanguageTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("config.data.title")))
						DrawDataTab(cfg);

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public static void DrawInterfaceTab(Configuration cfg) {

			ImGui.Text(Locale.GetString("config.interface.general.title"));

			ImGui.AlignTextToFramePadding();
			ImGui.Text(Locale.GetString("config.interface.general.openMethod.fieldLabel") + " ");
			ImGui.SameLine();
			var selectedOpenKtisisMethod = cfg.OpenKtisisMethod;
			ImGui.SetNextItemWidth(GuiHelpers.AvailableWidth(0));
			if (ImGui.BeginCombo("##OpenKtisisMethod", Locale.GetString($"config.interface.general.openMethod.{selectedOpenKtisisMethod}"))) {
				foreach (var openKtisisMethod in Enum.GetValues<OpenKtisisMethod>()) {
					if (ImGui.Selectable(Locale.GetString($"config.interface.general.openMethod.{openKtisisMethod}"), openKtisisMethod == selectedOpenKtisisMethod))
						cfg.OpenKtisisMethod = openKtisisMethod;
				}
				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox(Locale.GetString("config.interface.general.hideCharName"), ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			var censorNsfw = cfg.CensorNsfw;
			if (ImGui.Checkbox(Locale.GetString("config.interface.general.censorNSFW"), ref censorNsfw))
				cfg.CensorNsfw = censorNsfw;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text(Locale.GetString("config.interface.transformTable.title"));

			ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
			var transformTablePointDigits = cfg.TransformTableDigitPrecision;
			if (ImGui.DragInt(Locale.GetString("config.interface.transformTable.digitPrecision"), ref transformTablePointDigits, 1f, 1, 8))
				cfg.TransformTableDigitPrecision = transformTablePointDigits;

			var transformTableBaseSpeedPos = cfg.TransformTableBaseSpeedPos;
			if (ImGui.DragFloat(Locale.GetString("config.interface.transformTable.basePositionSpeed"), ref transformTableBaseSpeedPos, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedPos = transformTableBaseSpeedPos;

			var transformTableBaseSpeedRot = cfg.TransformTableBaseSpeedRot;
			if (ImGui.DragFloat(Locale.GetString("config.interface.transformTable.baseRotationSpeed"), ref transformTableBaseSpeedRot, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedRot = transformTableBaseSpeedRot;

			var transformTableBaseSpeedSca = cfg.TransformTableBaseSpeedSca;
			if (ImGui.DragFloat(Locale.GetString("config.interface.transformTable.baseScaleSpeed"), ref transformTableBaseSpeedSca, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedSca = transformTableBaseSpeedSca;

			var transformTableModifierMultCtrl = cfg.TransformTableModifierMultCtrl;
			if (ImGui.DragFloat(Locale.GetString("config.interface.transformTable.ctrlSpeedMult"), ref transformTableModifierMultCtrl, 1f, 0.00001f, 10000f, "%.4f",ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultCtrl = transformTableModifierMultCtrl;

			var transformTableModifierMultShift = cfg.TransformTableModifierMultShift;
			if (ImGui.DragFloat(Locale.GetString("config.interface.transformTable.shiftSpeedMult"), ref transformTableModifierMultShift, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultShift = transformTableModifierMultShift;

			var displayMultiplierInputs = cfg.TransformTableDisplayMultiplierInputs;
			if (ImGui.Checkbox(Locale.GetString("config.interface.transformTable.showSpeedMultiplierInputs"), ref displayMultiplierInputs))
				cfg.TransformTableDisplayMultiplierInputs = displayMultiplierInputs;
			
			var showToolbar = cfg.ShowToolbar;
			if (ImGui.Checkbox("Show Experimental Toolbar", ref showToolbar))
				cfg.ShowToolbar = showToolbar;
			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			if (ImGui.CollapsingHeader(Locale.GetString("config.overlay.skeleton.header"), ImGuiTreeNodeFlags.DefaultOpen)) {
				ImGui.Separator();
				var drawLines = cfg.DrawLinesOnSkeleton;
				if (ImGui.Checkbox(Locale.GetString("config.overlay.skeleton.drawLines"), ref drawLines))
					cfg.DrawLinesOnSkeleton = drawLines;

				var drawLinesGizmo = cfg.DrawLinesWithGizmo;
				if (ImGui.Checkbox(Locale.GetString("config.overlay.skeleton.drawLineWhenSelecting"), ref drawLinesGizmo))
					cfg.DrawLinesWithGizmo = drawLinesGizmo;

				var drawDotsGizmo = cfg.DrawDotsWithGizmo;
				if (ImGui.Checkbox(Locale.GetString("config.overlay.skeleton.drawDotsWhenSelecting"), ref drawDotsGizmo))
					cfg.DrawDotsWithGizmo = drawDotsGizmo;

				var dotRadius = cfg.SkeletonDotRadius;
				if (ImGui.SliderFloat(Locale.GetString("config.overlay.skeleton.dotRadius"), ref dotRadius, 0.01F, 15F, "%.1f"))
					cfg.SkeletonDotRadius = dotRadius;

				var lineThickness = cfg.SkeletonLineThickness;
				if (ImGui.SliderFloat(Locale.GetString("config.overlay.skeleton.lineThickness"), ref lineThickness, 0.01F, 15F, "%.1f"))
					cfg.SkeletonLineThickness = lineThickness;
				
				var lineOpacity = cfg.SkeletonLineOpacity;
				if (ImGui.SliderFloat(Locale.GetString("Lines_opacity"), ref lineOpacity, 0.01F, 1F, "%.2f"))
					cfg.SkeletonLineOpacity = lineOpacity;
				
				var lineOpacityWhileUsing = cfg.SkeletonLineOpacityWhileUsing;
				if (ImGui.SliderFloat(Locale.GetString("Lines_opacity_while_using"), ref lineOpacityWhileUsing, 0.01F, 1F, "%.2f"))
					cfg.SkeletonLineOpacityWhileUsing = lineOpacityWhileUsing;
			}
			if (ImGui.CollapsingHeader(Locale.GetString("config.overlay.boneColors.header"), ImGuiTreeNodeFlags.DefaultOpen)) {

				ImGui.Separator();

				bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
				if (GuiHelpers.IconButtonTooltip(cfg.LinkBoneCategoryColors ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink, linkBoneCategoriesColors ? Locale.GetString("config.overlay.boneColors.link.disable") : Locale.GetString("config.overlay.boneColors.link.enable")))
					cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, Locale.GetString("config.overlay.boneColors.erase.tooltip"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
					Vector4 eraseColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
					if (linkBoneCategoriesColors) {
						cfg.LinkedBoneCategoryColor = eraseColor;
					} else {
						foreach (Category category in Category.Categories.Values) {
							if (category.ShouldDisplay || cfg.BoneCategoryColors.ContainsKey(category.Name))
								cfg.BoneCategoryColors[category.Name] = eraseColor;
						}
					}
				}

				if (linkBoneCategoriesColors) {
					Vector4 linkedBoneColor = cfg.LinkedBoneCategoryColor;
					if (ImGui.ColorEdit4(Locale.GetString("config.overlay.boneColors.allEdit.label"), ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
						cfg.LinkedBoneCategoryColor = linkedBoneColor;
				} else {

					ImGui.SameLine();
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, Locale.GetString("config.overlay.boneColors.categories.reset.tooltip"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
						foreach ((string categoryName, Category category) in Category.Categories) {
							if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
								continue;
							cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
						}
					}

					ImGui.Text(Locale.GetString("config.overlay.Categories_colors"));

					if (!Components.Categories.DrawConfigList(cfg))
						ImGui.TextWrapped(Locale.GetString("config.overlay.categories.placeholderText"));

				}
			}
			if (ImGui.CollapsingHeader(Locale.GetString("config.overlay.bonePositions.header")))
				DrawBonesOffset(cfg);

			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox(Locale.GetString("config.gizmo.allowAxisFlip"), ref allowAxisFlip))
				cfg.AllowAxisFlip = allowAxisFlip;

			ImGui.EndTabItem();
		}

		// Language

		public static void DrawLanguageTab(Configuration cfg) {
			ImGui.Text("Disclaimer! These settings are currently only in place to test the WIP localization system.");
			ImGui.Text("Translation strings are not currently supported in most of the UI.");

			ImGui.Spacing();

			var selected = "";
			foreach (var lang in Locale.Languages) {
				if (lang == cfg.Localization) {
					selected = $"{lang}";
					break;
				}
			}

			if (ImGui.BeginCombo(Locale.GetString("config.language.select"), selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected))
						cfg.Localization = lang;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = cfg.TranslateBones;
			if (ImGui.Checkbox(Locale.GetString("config.language.translateBones"), ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}


		// input selector
		public static void DrawInputTab(Configuration cfg) {
			ImGui.Spacing();
			ImGui.Text(Locale.GetString("config.input.selectBehavior.title"));
			ImGui.Spacing();

			var disableChangeTargetOnLeftClick = cfg.DisableChangeTargetOnLeftClick;
			if (ImGui.Checkbox(Locale.GetString("config.input.selectBehavior.disableLeftClickTarget"), ref disableChangeTargetOnLeftClick))
				cfg.DisableChangeTargetOnLeftClick = disableChangeTargetOnLeftClick;

			var disableChangeTargetOnRightClick = cfg.DisableChangeTargetOnRightClick;
			if (ImGui.Checkbox(Locale.GetString("config.input.selectBehavior.disableRightClickTarget"), ref disableChangeTargetOnRightClick))
				cfg.DisableChangeTargetOnRightClick = disableChangeTargetOnRightClick;


			ImGui.Spacing();
			ImGui.Text(Locale.GetString("config.input.keybind.title"));
			ImGui.Spacing();

			// completely enable/disable keyboard shortcuts
			var enableKeybinds = cfg.EnableKeybinds;
			if(ImGui.Checkbox(Locale.GetString("config.input.keybind.enable"), ref enableKeybinds))
				cfg.EnableKeybinds = enableKeybinds;
			if (!cfg.EnableKeybinds) {
				ImGui.EndTabItem();
				return;
			}

			// display the currently pressed keys
			List<VirtualKey> pressDemo = Input.FallbackKey;
			foreach (var key in Enum.GetValues<VirtualKey>()) {
				if (!Services.KeyState.IsVirtualKeyValid(key)) continue;
				var state = Services.KeyState[key];
				if (state && pressDemo == Input.FallbackKey)
					pressDemo = new();
				if (state)
					pressDemo.Add(key);
			}

			ImGui.Text(Locale.GetString("config.input.keybind.assignByKeyPress.title"));
			ImGuiComponents.HelpMarker(Locale.GetString("config.input.keybind.assignByKeyPress.helpText"));
			ImGui.SameLine();
			ImGui.Text($":   {PrettyKeys(pressDemo)}");
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginChildFrame(74, new(-1, ImGui.GetFontSize() * 20));

			// key/Action table
			ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, ImGui.GetStyle().CellPadding * 3);
			if (ImGui.BeginTable("keybinds_table", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX)) {

				// display and configureheaders
				ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
				ImGui.TableSetupColumn(Locale.GetString("config.input.keybind.keybindTable.keys"));
				ImGui.TableSetupColumn(Locale.GetString("config.input.keybind.keybindTable.action"));
				ImGui.TableHeadersRow();

				foreach (var purpose in Input.PurposesWithCategories) {
					if ((int)purpose >= Input.FirstCategoryPurposeHold)
						if (Input.PurposesCategories.TryGetValue(purpose, out var category))
							if ((!cfg.KeyBinds.Any(t => t.Key == purpose) && !category.ShouldDisplay) || (cfg.CensorNsfw && category.IsNsfw)) continue;

					// get currently configured or default keys
					if (!Input.DefaultKeys.TryGetValue(purpose, out List<VirtualKey>? defaultKeys))
						defaultKeys = Input.FallbackKey;
					if (!cfg.KeyBinds.TryGetValue(purpose, out List<VirtualKey>? configuredKeys))
						configuredKeys = defaultKeys;

					ImGui.TableNextRow();
					var clickRow = false;

					// display the current key (config or default)
					ImGui.TableNextColumn();
					var configuredKeysPretty = PrettyKeys(configuredKeys);
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(configuredKeysPretty).X) / 2));
					ImGui.Selectable($"{configuredKeysPretty}##{purpose}", false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// display the purpose
					ImGui.TableNextColumn();
					ImGui.Selectable(Locale.GetInputPurposeName(purpose), false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// execute the change if clicked
					if (clickRow)
						if (pressDemo == defaultKeys) cfg.KeyBinds.Remove(purpose);
						else cfg.KeyBinds[purpose] = pressDemo;

				}
				ImGui.EndTable();
			}
			ImGui.PopStyleVar();
			ImGui.EndChildFrame();
			ImGui.EndTabItem();
		}
		private static string PrettyKeys(List<VirtualKey>  keys) => string.Join(" + ", keys.Select(k => VirtualKeyExtensions.GetFancyName(k)));

		public static void DrawDataTab(Configuration cfg) {
			ImGui.Spacing();
			var validGlamPlatesFound = GlamourDresser.CountValid();
			GuiHelpers.TextTooltip($"{Locale.GetString("config.data.glamourDresser.memoryCount")}{validGlamPlatesFound}  ", $"{Locale.GetString("config.data.glamourDresser.validCount.pre")}{validGlamPlatesFound} {Locale.GetString("config.data.glamourDresser.validCount.post")}");
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, Locale.GetString("config.data.sync.tooltip")))
				GlamourDresser.PopulatePlatesData();

			Components.Equipment.CreateGlamourQuestionPopup();

			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Trash, Locale.GetString("config.data.dispose.tooltip"))) {
				Sets.Dispose();
				cfg.GlamourPlateData = null;
			}
			ImGui.EndTabItem();
		}

		public static unsafe void DrawBonesOffset(Configuration cfg) {

			ImGui.Separator();
			var bone = Skeleton.GetSelectedBone();
			if (bone != null) {
				var targetBodyType = CustomOffset.GetRaceGenderFromActor(Ktisis.Target);
				var targetBoneOffset = CustomOffset.GetBoneOffset(bone);

				ImGui.Text($"Edit {targetBodyType}'s {bone.LocaleName}  ");

				if (!cfg.CustomBoneOffset.TryGetValue(targetBodyType, out var _))
					cfg.CustomBoneOffset.Add(targetBodyType,new());

				if (GuiHelpers.DragFloat3FillWidth($"##currentTargetOffset", false, null, ref targetBoneOffset, .00001f, "%.5f"))
					cfg.CustomBoneOffset[targetBodyType][bone.HkaBone.Name.String] = targetBoneOffset;
			} else {
				ImGuiComponents.HelpMarker("Select a Bone to start adjusting its position.");
			}

			ImGui.Spacing();

			foreach (var bt in cfg.CustomBoneOffset) {
				var bodyType = bt.Key;
				if (ImGui.CollapsingHeader($"{bodyType}")) {
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash,$"Hold Ctrl and Shift to drop all {bodyType} bone offsets.", default, $"dropall##{bodyType}"))
						cfg.CustomBoneOffset.Remove(bodyType);
					ImGui.SameLine();
					if (GuiHelpers.IconButton(FontAwesomeIcon.Clipboard, default, $"export##{bodyType}"))
						ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cfg.CustomBoneOffset[bodyType]))));
					ImGui.SameLine();
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Paste, $"Hold Ctrl and Shift to paste and replace all {bodyType} bone offsets.", default, $"pasteReplaceAll##{bodyType}")) {
						var parsedPasteAll = JsonConvert.DeserializeObject<Dictionary<string, Vector3>>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText())));
						if (parsedPasteAll != null)
							cfg.CustomBoneOffset[bodyType] = parsedPasteAll;
					}

					ImGuiComponents.HelpMarker("Tips:\n" +
						"Click on a row to copy it into clipboard" +
						"Ctrl + Shift + Right click to remove a row" +
						"The plus (+) button will insert a copied row");


					if (ImGui.BeginTable("offsetBonesTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX)) {

						ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
						ImGui.TableSetupColumn("Bone");
						ImGui.TableSetupColumn("X");
						ImGui.TableSetupColumn("Y");
						ImGui.TableSetupColumn("Z");
						ImGui.TableHeadersRow();

						foreach (var os in bt.Value) {
							var boneName = os.Key;
							var offsets = os.Value;
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							var isDeletable = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
							if(isDeletable) ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Workspace.Workspace.ColRed);
							if (ImGui.Selectable($"{Locale.GetBoneName(boneName)}##{bodyType}##customBoneOffset", false, ImGuiSelectableFlags.SpanAllColumns))
								ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((boneName,offsets)))));
							if (isDeletable) ImGui.PopStyleColor();
							if (isDeletable && ImGui.IsItemClicked(ImGuiMouseButton.Right))
								cfg.CustomBoneOffset[bodyType].Remove(boneName);
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.X:F6}");
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.Y:F6}");
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.Z:F6}");
						}
						ImGui.EndTable();
						if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus,"Add a line from clipboard.", default, $"{bodyType}##Clipboard##AddLine")) {
							var parsedPasteLine = JsonConvert.DeserializeObject<(string, Vector3)>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText()))) ;
							cfg.CustomBoneOffset[bodyType][parsedPasteLine.Item1] = parsedPasteLine.Item2;
						}
					}
				}
			}
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, $"Hold Ctrl and Shift to drop ALL bone offsets.", default, "dropAllOffset"))
				cfg.CustomBoneOffset = new();

			ImGui.Spacing();
		}

		// References

		public static void DrawReferencesTab(Configuration cfg) {
			ImGui.TextWrapped(Locale.GetString("config.references.explanation"));
			var alpha = cfg.ReferenceAlpha;
			if (ImGui.SliderFloat(Locale.GetString("config.references.imageAlpha"), ref alpha, 0.0f, 1.0f)) {
				cfg.ReferenceAlpha = alpha;
			}
			var hideDecoration = cfg.ReferenceHideDecoration;
			if (ImGui.Checkbox(Locale.GetString("config.references.hideWindowDecoration"), ref hideDecoration)) {
				cfg.ReferenceHideDecoration = hideDecoration;
			}
			ImGui.Text(Locale.GetString("config.references.images.title"));
			foreach (var (key, reference) in cfg.References) {
				ImGui.PushID(key);
				bool showing = reference.Showing;
				if (ImGui.Checkbox("##Showing", ref showing)) {
					reference.Showing = showing;
				}
				ImGui.SameLine();
				var buf = new string(reference.Path);
				if (ImGui.InputText("##Path", ref buf, 255, ImGuiInputTextFlags.EnterReturnsTrue) || ImGui.IsItemDeactivatedAfterEdit()) {
					TryChangeReference(cfg, key, buf);
				}
				ImGui.SameLine();
				if (GuiHelpers.IconButton(FontAwesomeIcon.File, ButtonSize)) {
					KtisisGui.FileDialogManager.OpenFileDialog(
						Locale.GetString("config.references.images.dialog.title"),
						Locale.GetString("config.references.images.dialog.filter")+ "{.gif,.jpg,.jpeg,.png}",
						(success, filePath) => {
							if (success) {
								TryChangeReference(cfg, key, filePath);
							}
						}
					);
				}
				ImGui.SameLine();
				if (GuiHelpers.IconButton(FontAwesomeIcon.Trash, ButtonSize)) {
					cfg.References.Remove(key);
					References.DisposeUnreferencedTextures(cfg);
				}
				ImGui.PopID();
			}

			if (GuiHelpers.IconButton(FontAwesomeIcon.Plus, ButtonSize)) {
				cfg.References[cfg.NextReferenceKey] = new ReferenceInfo { Showing = true };
			}
			ImGui.SameLine();
			ImGui.Text(Locale.GetString("config.references.images.addText"));

			ImGui.EndTabItem();
		}

		public static bool TryChangeReference(Configuration cfg, int key, string newPath) {
			try {
				var texture = Ktisis.UiBuilder.LoadImage(newPath);
				cfg.References[key] = new ReferenceInfo {
					Path = newPath,
					Showing = true,
				};
				References.Textures[newPath] = texture;
				Logger.Information("Successfully loaded reference image {0}", newPath);
				References.DisposeUnreferencedTextures(cfg);
				return true;
			} catch (Exception e) {
				Logger.Error(e, "Failed to load reference image {0}", newPath);
				return false;
			}
		}
	}
}
