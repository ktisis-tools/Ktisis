using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Logging;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Ktisis;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Overlay {
	public sealed class SkeletonEditor {
		public GameGui Gui;
		public ObjectTable ObjectTable;

		public GameObject? Subject;
		public List<BoneList>? Skeleton;

		// TODO: Clean this up
		public (int, int) BoneSelection;
		public Transform BoneTranslate;
		public SharpDX.Matrix BoneMatrix;
		public SharpDX.Matrix DeltaMatrix;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr GetMatrixDelegate();
		internal GetMatrixDelegate GetMatrix;

		float[] cameraView = {
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};

		public unsafe SkeletonEditor(Ktisis plugin, GameObject? subject) {
			Gui = plugin.GameGui;
			ObjectTable = plugin.ObjectTable;

			Subject = subject;
			BoneSelection = (-1, -1);

			var matrixAddr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
			GetMatrix = Marshal.GetDelegateForFunctionPointer<GetMatrixDelegate>(matrixAddr);
		}

		public unsafe ActorModel* GetSubjectModel() {
			return ((Actor*)Subject?.Address)->Model;
		}

		public unsafe void BuildSkeleton() {
			Skeleton = new List<BoneList>();

			var model = GetSubjectModel();
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

		public unsafe void Draw(ImDrawListPtr draw) {
			var tarSys = TargetSystem.Instance();
			if (tarSys == null)
				return;

			var target = ObjectTable.CreateObjectReference((IntPtr)(tarSys->GPoseTarget));
			if (target == null || Subject == null || Subject.Address != target.Address) {
				Subject = target;
				if (Subject != null)
					BuildSkeleton();
			}

			if (Subject == null)
				return;
			if (Skeleton == null)
				return;

			var model = GetSubjectModel();
			if (model == null)
				return;

			var cam = CameraManager.Instance()->Camera;
			if (cam == null)
				return;

			var hasBoneHovered = false;

			foreach (BoneList bones in Skeleton) {
				foreach (Bone bone in bones) {
					if (bone.IsRoot)
						continue;

					var pair = (bones.Id, bone.Index);

					var worldPos = model->Position + bone.Rotate(model->Rotation) * model->Height;
					Gui.WorldToScreen(worldPos, out var pos);

					if (bone.ParentId > 0) { // Lines
						var parent = bones.GetParentOf(bone);
						var parentPos = model->Position + parent.Rotate(model->Rotation) * model->Height;

						Gui.WorldToScreen(parentPos, out var pPos);
						draw.AddLine(pos, pPos, 0xffffffff);
					}

					if (pair == BoneSelection) { // Gizmo
						var io = ImGui.GetIO();
						var wp = ImGui.GetWindowPos();

						var matrix = (WorldMatrix*)GetMatrix();
						if (matrix == null)
							return;

						ImGuizmo.BeginFrame();
						ImGuizmo.SetDrawlist();
						ImGuizmo.SetRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);
						ImGuizmo.Manipulate(ref matrix->Projection.M11, ref cameraView[0], OPERATION.TRANSLATE, MODE.LOCAL, ref BoneMatrix.M11, ref DeltaMatrix.M11);

						// TODO: Streamline this.

						var delta = new Transform();
						ImGuizmo.DecomposeMatrixToComponents(ref DeltaMatrix.M11, ref delta.Translate.X, ref delta.Rotate.X, ref delta.Scale.X);

						var inverse = Quaternion.Inverse(model->Rotation);
						delta.Translate = Vector4.Transform(
							delta.Translate,
							inverse
						) / model->Height;

						bone.TransformBone(delta, Skeleton);
					} else { // Dot
						var radius = Math.Max(3.0f, 10.0f - cam->Distance);

						var area = new Vector2(radius, radius);
						var rectMin = pos - area;
						var rectMax = pos + area;

						var hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
						if (hovered) {
							hasBoneHovered = true;
							if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
								BoneSelection = (bones.Id, bone.Index);
								BoneTranslate = bone.Transform;
								DeltaMatrix = default(SharpDX.Matrix);

								ImGuizmo.RecomposeMatrixFromComponents(
									ref worldPos.X,
									ref bone.Transform.Rotate.X,
									ref bone.Transform.Scale.X,
									ref BoneMatrix.M11
								);
							} else {
								var name = bone.HkaBone.Name;
								if (name != null)
									draw.AddText(pos, 0xffffffff, name);
							}
						}

						draw.AddCircleFilled(pos, Math.Max(2.0f, 8.0f - cam->Distance), hovered ? 0xffffffff : 0xb0ffffff, 100);
					}
				}

				//break;
			}

			if (hasBoneHovered)
				ImGui.SetNextFrameWantCaptureMouse(hasBoneHovered);
		}
	}
}