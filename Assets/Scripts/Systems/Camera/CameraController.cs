using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;


namespace Apes.Camera
{
	public class CameraController : MonoSingleton<CameraController>
	{
		private Mob mob;

		/// <summary>
		/// How much the camera position is influenced by the cursor.
		/// </summary>
		[SerializeField]
		private float cursorWeight = .33f;

		public bool followingCursor = true;

		/// <summary>
		/// Smoothing applied to the camera movement in game.
		/// </summary>
		[SerializeField]
		private float inGameSmoothing = .04f;
		/// <summary>
		/// Smoothing applied to the camera movement during scenes.
		/// </summary>
		[SerializeField]
		private float sceneSmoothing = .5f;
		/// <summary>
		/// Current smoothing.
		/// </summary>
		private float followSmoothing = .1f;

		private bool __inScene = false;
		public bool InScene
		{
			get => __inScene;
			set
			{
				__inScene = value;
				followSmoothing = __inScene ? sceneSmoothing : inGameSmoothing;
				followingCursor = !__inScene;
			}
		}

		[SerializeField]
		private float positionTolerance = .02f;

		[SerializeField, Min(.01f)]
		private float defaultDistance = 20f;
		[SerializeField]
		private float defaultFOV = 30f;

		/// <summary>
		/// The camera assigned to this controller.
		/// </summary>
		public UnityEngine.Camera Camera => UnityEngine.Camera.main;

		// public UnityEngine.U2D.PixelPerfectCamera PixelPerfectCamera => Camera.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();

		public float FOV
		{
			get => Camera.fieldOfView;
			set => Camera.fieldOfView = value;
		}

		[SerializeField]
		private float defaultOrthographicSize = 7.5f;
		public static readonly float minOrthographicSize = .5f;
		public static readonly float maxOrthographicSize = 10f;
		public float OrthographicSize
		{
			get => Camera.orthographicSize;
			set => Camera.orthographicSize = Mathf.Clamp(value, minOrthographicSize, maxOrthographicSize);
		}

		public float Distance
		{
			get => Vector3.Distance(Camera.transform.position, transform.position);
			set => Camera.transform.position = transform.position - Camera.transform.forward * value;
		}

		private Vector3 currentVelocity = Vector3.zero;

		public void SetTrackedMob(Mob mob)
		{
			if (this.mob)
			{
				mob.CanHideWalls = false;
				RemovePOI(this.mob.transform);
			}

			this.mob = mob;
			AddPOI(mob.transform);
			mob.CanHideWalls = true;
		}

		public void Move(Vector2 shift) => transform.position = LimitCamera(transform.position + (Vector3)shift);

		private readonly Dictionary<Transform, float> points = new();

		private void Start()
		{
			FOV = defaultFOV;
			Distance = defaultDistance;
			OrthographicSize = defaultOrthographicSize;
		}

		private void Update() => Follow();

		private void Follow()
		{
			int totalPoints = points.Count;
			if (totalPoints <= 0)
				return;

			Vector3 cursorPos = Vector3.zero;
			bool cursorActive = followingCursor && !Game.Paused;
			if (cursorActive)
			{
				totalPoints++;
				cursorPos = GetWorldCursorPos();
			}

			Vector3 center = cursorPos;
			foreach (Transform pointTransform in points.Keys)
				center += pointTransform.position;
			center /= totalPoints;

			Vector3 shift = cursorActive ? (cursorPos - center) * cursorWeight : Vector3.zero;
			foreach (KeyValuePair<Transform, float> poi in points)
				shift += (poi.Key.position - center) * poi.Value;
			shift /= totalPoints;

			Vector3 target = center + shift;

			target = LimitCamera(target);

			if (Vector3.Distance(target, transform.position) < positionTolerance)
				return;

			transform.position = Vector3.SmoothDamp(
				transform.position,
				target,
				ref currentVelocity,
				followSmoothing
			);
		}

		/// <summary>
		/// Projects cursor to the world floor plane and returns the result.
		/// </summary>
		/// <param name="heightOffset">Vertical offset of the plane the cursor is projected onto.</param>
		/// <returns>Cursor position in the world or Vector3.zero in case of error.</returns>
		public static Vector3 GetWorldCursorPos(float heightOffset = 0)
		{
			Plane plane = new Plane(Vector3.up, heightOffset);
			Ray ray = Instance.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
			if (plane.Raycast(ray, out float distance))
				return ray.GetPoint(distance);
			return Vector3.zero;
		}

		public void SetOnlyPOI(GameObject poi) => SetOnlyPOI(poi.transform);
		public void SetOnlyPOI(Transform poi)
		{
			points.Clear();
			followingCursor = false;
			AddPOI(poi);
		}

		public void ResetPOI()
		{
			points.Clear();
			if (mob)
				SetTrackedMob(mob);
		}

		// There are separate methods because unity won't serialize 2 arguments by default.
		public void AddPOI(GameObject poi) => AddPOI(poi.transform, 1f);
		public void AddPOI(GameObject poi, float weight) => AddPOI(poi.transform, weight);
		public void AddPOI(Transform poi, float weight = 1f)
		{
			if (!points.ContainsKey(poi))
				points.Add(poi, weight);
		}

		public bool RemovePOI(GameObject poi) => RemovePOI(poi.transform);
		public bool RemovePOI(Transform poi) => points.Remove(poi);

		public Vector3 LimitCamera(Vector3 target)//, RectInt bounds)
		{
				//Vector2 origin = bounds.transform.position;
				//Vector2 size = (Vector2)bounds.WorldSize / bounds.cellsPerMeter;

				//target.x = Mathf.Clamp(target.x, origin.x, size.x);
				//target.y = Mathf.Clamp(target.y, origin.y, size.y);
			
			return target;
		}
	}
}