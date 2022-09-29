using UnityEngine;
using Unity.Entities;
using Apes.Input;

namespace Verse
{
	public partial class SandboxPaintingSystem : SystemBase
	{
		public InputActions Actions => PlayerInput.Actions;

		private Entity space;

		protected override void OnCreate()
		{
			base.OnCreate();

			Actions.Sandbox.BrushSize.performed += (ctx) => InputBrushSize(ctx.ReadValue<float>());
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			space = GetSingletonEntity<SpaceData.Size>();
		}

		protected override void OnUpdate()
		{
			if (Actions.Sandbox.Paint.ReadValue<float>() > 0f)
			{
				Paint(SpaceCursorSystem.Coord, GetSingleton<SandboxData.Brush>().size, GetSingleton<SandboxData.PaintingMatter>().matter);
			}
			else if (Actions.Sandbox.Clear.ReadValue<float>() > 0f)
			{
				Erase(SpaceCursorSystem.Coord, GetSingleton<SandboxData.Brush>().size);
			}
		}

		public void Paint(Vector2Int center, int brushSize, Entity matter)
		{
			if (matter == Entity.Null)
				return;

			foreach (Vector2Int spaceCoord in Enumerators.GetCircle(center: center, radius: brushSize))
				Space.CreateAtom(EntityManager, space, matter, spaceCoord);

			UpdateBrushSquare(center, brushSize);
		}

		public void Erase(Vector2Int center, int brushSize)
		{
			//foreach (Vector2Int coord in Enumerators.GetCircle(center: center, radius: brushSize))
			//{
			//    if (space.HasCoord(coord) && space[coord])
			//        space.DestroyAtom(coord, updatePhysics: false, updateTexture: false);
			//}

			//UpdateBrushSquare(center);
		}

		private void UpdateBrushSquare(Vector2Int center, int brushSize)
		{
			int inflatedSize = brushSize + 1;
			int doubleInflated = (inflatedSize << 1) + 1;

			RectInt rect = new(
				center.x - inflatedSize,
				center.y - inflatedSize,
				doubleInflated,
				doubleInflated
			);

			Space.MarkDirty(EntityManager, space, rect, safe: true);
		}

		private void InputBrushSize(float inputValue)
		{
			int size = GetSingleton<SandboxData.Brush>().size;
			size = Mathf.Clamp(size + (int)Mathf.Sign(inputValue), 0, Space.chunkSize);
			SetSingleton(new SandboxData.Brush { size = size });
		}

		public partial struct PaintJob : IJobEntity
		{
		}
	}
}
