using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verse
{
	[CreateAssetMenu(fileName = "ReactionData", menuName = "APEZ/Matter", order = 1)]
	public class Reaction : ScriptableObject
	{
		public enum ReactionType
		{
			Melting,
			Solidification,
			Evaporation,
			Condensation,
			Hydrolysis,
			Galvanizing
		}
	}
}