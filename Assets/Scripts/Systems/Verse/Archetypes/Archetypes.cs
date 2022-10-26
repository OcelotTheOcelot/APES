using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public static class Archetypes
{
	public static EntityArchetype Atom { get; private set; }

	public static void RegisterArchetypes(EntityArchetype atom)
	{
		Atom = atom;
	}
}
