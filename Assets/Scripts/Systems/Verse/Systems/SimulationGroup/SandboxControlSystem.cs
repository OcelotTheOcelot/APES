using UnityEngine;
using Unity.Entities;
using Apes.Input;
using Apes.Camera;
using Apes.UI;

namespace Verse
{
	public partial class SandboxControlSystem : SystemBase
	{
		public InputActions Actions => PlayerInput.Actions;

		private float cameraMoveSpeed;
		private float cameraMoveSpeedZoomCorrection;
		private float cameraDragSpeed;
		private float cameraZoomSpeed;
		private float cameraZoomSmoothing;

		private bool isPanning;

		private float zoomFraction = 1f;
		private float zoomDifference = 1f;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<Sandbox.Controls>();

			zoomDifference = CameraController.maxOrthographicSize - CameraController.minOrthographicSize;
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			var controls = GetSingleton<Sandbox.Controls>();

			cameraMoveSpeed = controls.cameraMoveSpeed;
			cameraDragSpeed = controls.cameraPanningSpeed;
			cameraZoomSpeed = controls.cameraZoomSpeed;
			cameraZoomSmoothing = controls.cameraZoomSmoothing;
			cameraMoveSpeedZoomCorrection = controls.cameraMoveSpeedZoomCorrection;

			Coord regionCount = GetSingleton<Space.Bounds>().spaceGridBounds.Size;

			CameraController.Instance.transform.position = Space.regionSize * .5f * (Vector2)(regionCount * Space.metersPerCell);
		}

		protected override void OnUpdate()
		{
			SandboxUI.Instance.CursorPositionText = $"[{SpaceCursorSystem.Coord.x}; {SpaceCursorSystem.Coord.y}]";

			float delta = World.Time.DeltaTime;

			InputMove(Actions.Sandbox.MoveCamera.ReadValue<Vector2>(), delta);
			InputZoom(Actions.Sandbox.ZoomCamera.ReadValue<float>(), delta);
		}

		private void InputZoom(float inputZoomValue, float delta)
		{
			float direction = Mathf.Clamp(inputZoomValue, -1f, 1f);
			zoomFraction = Mathf.Clamp01(zoomFraction - direction * cameraZoomSpeed * delta);

			float size = CameraController.minOrthographicSize + zoomDifference * Mathf.Pow(zoomFraction, cameraZoomSmoothing);
			DebugUI.Instance.MessageText = $"Zoom: {size} ({CameraController.minOrthographicSize} + {zoomDifference} * {zoomFraction}^{cameraZoomSmoothing})";
			CameraController.Instance.OrthographicSize = size;

			//Vector2 difference = Camera.main.ScreenToWorldPoint(cursorPos) - CameraController.Instance.transform.position;
			//CameraController.Instance.Move(difference * size);
		}

		private void InputMove(Vector2 inputMovementVector, float delta)
		{
			Vector2 movement = inputMovementVector * (cameraMoveSpeed + (zoomFraction * cameraMoveSpeedZoomCorrection));

			if (isPanning)
				movement -= cameraDragSpeed * CameraController.Instance.OrthographicSize * Actions.Sandbox.CameraDelta.ReadValue<Vector2>();

			CameraController.Instance.Move(delta * movement);
		}
	}
}
