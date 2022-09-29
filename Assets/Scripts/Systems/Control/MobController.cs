using UnityEngine;

namespace Apes.Control
{
	public abstract class MobController : MonoBehaviour
	{
		/// <summary>
		/// Tells the controller what mob should be possessed as the level starts.
		/// Needed only in specific cases to possess mobs in the editor where the controller can't be attached to the mob itself.
		/// </summary>
		public Mob possessAtStart;
		public Mob Possessed { get; protected set; }

		protected static int TotalControllers { get; private set; } = 0;
		public int Id { get; private set; }

		public Vector3 movement;

		protected virtual void Awake()
		{
			Id = TotalControllers++;
		}

		protected virtual void Start()
		{
			if ((possessAtStart is Mob mob) || TryGetComponent(out mob))
				PossessMob(mob);
		}

		/// <summary>
		/// Assumes control of the mob.
		/// </summary>
		/// <param name="mob">The mob to take control of.</param>
		public virtual void PossessMob(Mob mob)
		{
			mob.SetPossessed(this);
			Possessed = mob;
			Debug.Log($"Controller {Id} possessed {mob}.");
		}

		private void Update()
		{
			if (!Possessed)
				return;

			OnUpdate(Time.deltaTime);
		}

		protected virtual void OnUpdate(float delta)
		{
		}

		protected virtual Vector3 UpdateMovementInput() => Vector3.zero;

		private void FixedUpdate()
		{
			if (Possessed)
				Possessed.Move(Time.fixedDeltaTime, movement);
		}
	}
}