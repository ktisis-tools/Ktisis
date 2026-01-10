using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Interop;

using Ktisis.Common.Utility;

using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

namespace Ktisis.Structs.Objects;

// https://github.com/ktisis-tools/ffxiv-pathfinder/blob/main/Pathfinder/Objects/Data/WorldObject.cs

public struct WorldObject : IEquatable<WorldObject> {
	private readonly Pointer<Object> Pointer;

	public nint Address { get; }
	public Transform InitialTransform { get; }

public unsafe WorldObject(Pointer<Object> ptr) {
		this.Pointer = ptr;
		this.Address = (nint)ptr.Value;
		this.InitialTransform = new Transform(
			this.Pointer.Value->Position,
			this.Pointer.Value->Rotation,
			this.Pointer.Value->Scale
		);
	}

	public unsafe WorldObject(Object* ptr) {
		this.Pointer = ptr;
		this.Address = (nint)ptr;
		this.InitialTransform = new Transform(
			this.Pointer.Value->Position,
			this.Pointer.Value->Rotation,
			this.Pointer.Value->Scale
		);
	}

	// Data wrappers

	public unsafe ObjectType ObjectType => this.Pointer.Value->GetObjectType();

	// Enumerate children & siblings

	private unsafe WorldObject? GetFirstChild() {
		if (this.Pointer.Value == null) return null;
		var ptr = this.Pointer.Value;
		var child = ptr->ChildObject;
		return child != null && child != ptr ? new WorldObject(child) : null;
	}

	private unsafe WorldObject? NextSibling() {
		if (this.Pointer.Value == null) return null;
		var ptr = this.Pointer.Value;
		var sibling = ptr->NextSiblingObject;
		if (sibling == null || sibling == ptr)
			return null;
		return new WorldObject(sibling);
	}

	public IEnumerable<WorldObject> GetChildren() {
		var child = this.GetFirstChild();
		if (child == null) yield break;
		yield return child.Value;

		var firstSibling = child.Value.NextSibling();
		var sibling = firstSibling;
		while (sibling != null && sibling.Value.Address != this.Address && sibling.Value.Address != child.Value.Address) {
			yield return sibling.Value;
			sibling = sibling.Value.NextSibling();
			if (sibling?.Address == firstSibling?.Address)
				break;
		}
	}

	public IEnumerable<WorldObject> GetSiblings() {
		var sibling = this.NextSibling();

		while (sibling != null && sibling.Value.Address != this.Address) {
			yield return sibling.Value;
			sibling = sibling.Value.NextSibling();
		}
	}

	public bool Equals(WorldObject other) => this.Address == other.Address;
}
