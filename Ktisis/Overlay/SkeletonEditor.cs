using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Interop;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Overlay {
	public class SkeletonEditor {
		public bool Visible = true;

		public IntPtr? Subject;
		public List<BoneList>? Skeleton;

		public BoneSelector BoneSelector;
		public BoneMod BoneMod;

		public unsafe WorldMatrix* matrix;

		float[] cameraView = {
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};

		public bool HasSelected() => BoneSelector.Current != (-1, -1);

		// Controls

		public OPERATION GizmoOp = OPERATION.UNIVERSAL;
		public MODE Gizmode = MODE.LOCAL; // TODO: Improve this.

		// Constructor

		public unsafe SkeletonEditor(GameObject? subject = null) {
			//Subject = subject;

			BoneSelector = new BoneSelector();
			BoneMod = new BoneMod();

			matrix = (WorldMatrix*)CameraHooks.GetMatrix!();
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Get ActorModel

		public unsafe ActorModel* GetTargetModel() {
			return ((Actor*)Ktisis.GPoseTarget?.Address)->Model;
		}

		// Bone selection

		public unsafe void SelectBone(Bone bone) {
			var model = GetTargetModel();
			if (model == null) return;

			BoneSelector.Current = (bone.BoneList.Id, bone.Index);
			BoneMod.SnapshotBone(bone, model, Gizmode);
		}


		public unsafe Bone? GetSelectedBone()
		{
			(int ListId, int Index) = BoneSelector.Current;

			if (Skeleton == null) return null;
			foreach (BoneList boneList in Skeleton)
				if(boneList.Id == ListId)
					foreach (Bone bone in boneList)
						if (bone.Index == Index) return bone;

			return null;
		}

		// Build skeleton

		public unsafe void BuildSkeleton() {
			Skeleton = new List<BoneList>();

			var model = GetTargetModel();
			if (model == null)
				return;

			var linkList = new Dictionary<string, List<int>>(); // name : [index]

			// Create BoneLists

			var list = *model->HkaIndex;
			for (int i = 0; i < list.Count; i++) {
				var index = list[i];
				if (index.Pose == null)
					continue;

				var bones = new BoneList(i, index.Pose);

				var first = bones[0];
				first.IsRoot = true;

				// Is linked
				if (i > 0) {
					var firstName = first.HkaBone.Name!;

					if (!linkList.ContainsKey(firstName))
						linkList.Add(firstName, new List<int>());
					linkList[firstName].Add(i);
				}

				Skeleton.Add(bones);
			}

			// Set LinkedTo

			foreach (Bone bone in Skeleton[0]) {
				var name = bone.HkaBone.Name!;
				if (linkList.ContainsKey(name))
					bone.LinkedTo = linkList[name];
			}
		}

		// Reset state

		public void ResetState() {
			Skeleton = null;
			BoneSelector.ResetState();
		}

		public void SelectActorTarget()
		{
			ResetState();

			// TODO: place and render gizmo on actor
		}

		// Draw

		public unsafe void Draw() {
			if (!Visible || !Ktisis.Configuration.ShowSkeleton)
				return;

			if (!Ktisis.IsInGPose)
				return;

			var target = Ktisis.GPoseTarget;
			if (target == null) {
				Subject = null;
				return;
			}

			if (Subject != target.Address || Skeleton == null) {
				Subject = target!.Address;
				BuildSkeleton();
			}

			if (Subject == null || Skeleton == null)
				return;

			// Create window & fetch draw list

			Overlay.Begin();
			DrawSkeleton();
			Overlay.End();
		}

		// Draw skeleton

		public unsafe void DrawSkeleton() {
			var model = GetTargetModel();
			if (model == null)
				return;

			var cam = Dalamud.Camera->Camera;

			var draw = ImGui.GetWindowDrawList();

			var hoveredBones = new List<(int ListId, int Index)>();

			foreach (BoneList bones in Skeleton!) {
				foreach (Bone bone in bones) {
					if (bone.IsRoot)
						continue;

					uint boneColor = ImGui.GetColorU32(Ktisis.Configuration.GetCategoryColor(bone));

					var pair = (bones.Id, bone.Index);

					var worldPos = model->Position + bone.Rotate(model->Rotation) * model->Height;
					Dalamud.GameGui.WorldToScreen(worldPos, out var pos);

					if (Ktisis.Configuration.IsBoneVisible(bone)) {
						if (bone.ParentId > 0) { // Lines
							var parent = bone.GetParent()!;
							var parentPos = model->Position + parent.Rotate(model->Rotation) * model->Height;

							Dalamud.GameGui.WorldToScreen(parentPos, out var pPos);
							float lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / cam->Distance * 2.0f);

							draw.AddLine(pos, pPos, boneColor, lineThickness);
						}
					}

					if (pair == BoneSelector.Current) { // Gizmo
						var io = ImGui.GetIO();
						var wp = ImGui.GetWindowPos();

						if (matrix == null)
							return;

						ImGuizmo.BeginFrame();
						ImGuizmo.SetDrawlist();
						ImGuizmo.SetRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);

						ImGuizmo.AllowAxisFlip(Ktisis.Configuration.AllowAxisFlip);

						ImGuizmo.Manipulate(
							ref matrix->Projection.M11,
							ref cameraView[0],
							GizmoOp,
							Gizmode,
							ref BoneMod.BoneMatrix.M11,
							ref BoneMod.DeltaMatrix.M11
						);

						// TODO: Streamline this.

						//BoneMod.SnapshotBone(bone, model);
						BoneMod.ApplyDelta(bone, Skeleton);

					} else if (Ktisis.Configuration.IsBoneVisible(bone)) { // Dot
						var dist = *(float*)((IntPtr)cam + 0x17c); // https://github.com/aers/FFXIVClientStructs/pull/254
						var radius = Math.Max(3.0f, (10.0f - dist) * (Ktisis.Configuration.SkeletonLineThickness / 5f));
						var dotRadius = Math.Max(2.0f, (15.0f - dist) * (Ktisis.Configuration.SkeletonLineThickness / 5f));

						var area = new Vector2(radius, radius);
						var rectMin = pos - area;
						var rectMax = pos + area;

						var hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
						if (hovered)
							hoveredBones.Add(pair);

						draw.AddCircleFilled(pos, dotRadius, hovered ? (boneColor | 0xff000000) : boneColor, 100);
					}
				}

				//break;
			}

			if (hoveredBones.Count > 0)
				BoneSelector.Draw(this, hoveredBones);
		}
	}
}
