using UnityEngine;
using System;
using Apes.Control;

namespace Apes
{
	public abstract class Mob : DynamicObject
	{
		//public event Action<Item> OnPickedUpItem;
		//public event Action OnDroppedItem;
		public event Action OnHealthChanged;
		//public event Action<Mob> OnDamaged;
		//public event Action<Mob> OnDefeated;

		[field: SerializeField]
		public virtual float MaxHealth { get; set; } = 100;

		private float __health;
		public float Health
		{
			get => __health;
			protected set
			{
				__health = value;
				OnHealthChanged?.Invoke();
			}
		}

		protected Animator Animator { get; private set; }

		public virtual Transform ItemSocket => transform;

		public MobController Controller { get; private set; }
		public bool IsPlayer => PlayerController.Instance.Possessed == this;

		[field: SerializeField]
		public float MoveSpeed { get; private set; } = 250f;

		public virtual Vector3 AimPos { get; set; }
		private Vector3 activeDirection;

		protected override void Awake()
		{
			base.Awake();

			Health = MaxHealth;
			Animator = GetComponentInChildren<Animator>();
		}

		//public virtual void TakeDamage(Damage damage)
		//{
		//	if (!Alive || invincibility > 0f)
		//		return;

		//	string message = $"{this} took {damage.amount} points of {damage.type} damage";
		//	if (damage.inflictor)
		//		message += $" from {damage.inflictor}";
		//	message += ".";
		//	Debug.Log(message);

		//	OnDamaged?.Invoke(this);

		//	if ((Health -= damage.amount) < 0f)
		//		Die(damage);
		//}

		/// <summary>
		/// Handles the mob's active movement.
		/// </summary>
		/// <param name="delta">Delta between two frames.</param>
		/// <param name="direction">Vector describing the movement direction of the mob.
		/// Pass vector with near zero magnitude to make the mob stand.</param>
		/// <param name="affectY">If the request should influence the mob's vertical movement.</param>
		public virtual void Move(
			float delta,
			Vector3 direction,
			bool affectY = false
		)
		{
			//if (!CanMoveActively)
			//return;

			if (direction.magnitude > 1f)
				direction /= direction.magnitude;  // direction.Normalize();
			activeDirection = direction;

			Vector3 targetVelocity = delta * MoveSpeed * direction;
			if (!affectY)
				targetVelocity.y = Body.velocity.y;

			Body.velocity = targetVelocity;
		}

		/// <summary>
		/// Makes the mob possessed by the provided controller.
		/// </summary>
		/// <param name="controller">The controller that should possess the mob.</param>
		/// <returns>true if the mob has been possessed succesfully, false otherwise.</returns>
		public bool SetPossessed(MobController controller)
		{
			if (Controller)
				return false;
			Controller = controller;
			return true;
		}
	}
}