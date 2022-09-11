﻿using System;
using System.Collections;
using System.Collections.Generic;
using Ktisis.Structs.Havok;
using Ktisis.Structs.Bones;

namespace Ktisis.Structs {
	public unsafe class BoneList : IEnumerable {
		public int Id = -1;

		public List<Bone> Bones;

		public HkaSkeleton Skeleton;
		public ShitVecReversed<Transform> Transforms;

		// Constructors

		public BoneList(HkaPose* pose) {
			Skeleton = *pose->Skeleton;
			Transforms = pose->Transforms;

			Bones = new List<Bone>();
			for (int i = 0; i < Skeleton.Bones.Count; i++) {
				var bone = new Bone(this, i);
				Bones.Add(bone);
			}
		}

		public BoneList(int id, HkaPose* pose) : this(pose) {
			Id = id;
		}

		// Find parent

		public Bone? GetParentOf(Bone bone) {
			var id = bone.ParentId;
			return id >= 0 && id < Bones.Count ? this[id] : null;
		}

		// Children

		// Get direct children of this node
		public List<Bone> GetChildrenDirect(Bone bone) {
			var children = new List<Bone>();
			foreach (Bone v in this) {
				if (v.ParentId == bone.Index)
					children.Add(v);
			}
			return children;
		}

		// Get all descendents of this node
		public void GetChildrenRecursive(Bone bone, ref List<Bone> children) {
			foreach (Bone v in this) {
				if (v.ParentId == bone.Index) {
					children.Add(v);
					GetChildrenRecursive(v, ref children);
				}
			}
		}

		public List<Bone> GetParentsRecursive(Bone bone) {
			var parents = new List<Bone>();
			// this is the most unoptimised algorithm ever
			var pid = bone.ParentId;
			while (true) {
				// Target bone is null, break.
				if (pid < 0)
					break;
				// Iterate bones to find parent
				var found = false;
				foreach (Bone v in this) {
					// If parent, add to parents list, set pid and find next parent.
					if (v.Index == pid) {
						found = true;
						pid = v.ParentId;
						parents.Add(v);
						break;
					}
				}
				// No parent found, break.
				if (!found)
					break;
			}
			return parents;
		}

		// Enumerator

		public Bone GetAndUpdate(int index) {
			var bone = Bones[index];
			bone.UpdateTransform(this);
			return bone;
		}

		public Bone this[int index] {
			get => GetAndUpdate(index);
			set => new NotImplementedException();
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Skeleton.Bones.Count; i++)
				yield return this[i];
		}
	}
}
