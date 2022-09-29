using Apes.Input;
using Apes.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

namespace Verse
{
	public partial class SpaceCursorSystem : SystemBase
	{
		public static Vector2Int Coord
		{
			get;
			private set;
		}

		private InputActions actions;

		protected override void OnCreate()
		{
			base.OnCreate();

			actions = PlayerInput.Actions;
		}

		protected override void OnUpdate()
		{
			Vector2 cursorPos = actions.Global.Position.ReadValue<Vector2>();

			Entities.WithAll<SpaceData.Size>().ForEach(
				(in Translation translation, in SpaceData.Size sizeData) =>
				{
					Coord = Space.WorldToSpace(
						translation, sizeData,
						Camera.main.ScreenToWorldPoint(cursorPos)
					);
				}
			).Run();
		}
	}
}