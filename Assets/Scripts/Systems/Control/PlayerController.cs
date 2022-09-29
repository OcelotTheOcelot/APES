using Apes.Camera;
using Apes.Input;
using Input;
using System;
using UnityEngine;


namespace Apes.Control
{
	public class PlayerController : MobController
	{
		public event Action<Mob> OnPossessed;

		private static PlayerController inst;
		public static PlayerController Instance
		{
			get
			{
				if (inst)
					return inst;

				Type type = typeof(PlayerController);
				inst = (PlayerController)FindObjectOfType(type);
				if (!inst)
					Debug.LogWarning($"В сцене нужен экземпляр {type}, но он отсутствует.");

				return inst;
			}
		}

		/// <summary>
		/// The interactable entity currently selected by the Possessed.
		/// </summary>
		public Interaction SelectedEntity { get; protected set; }
		private readonly Collider[] colliderBuffer = new Collider[16];
		private LayerMask interactableMask;

		public InputActions Actions => PlayerInput.Actions;

		/// <summary>
		/// The angular size of selection sector.
		/// </summary>
		public float selectionScope = 45f;

		/// <summary>
		/// The radius of the selection sector.
		/// </summary>
		public float selectionDistance = 1.25f;

		protected override void Awake()
		{
			base.Awake();

			interactableMask = LayerMask.GetMask(new string[] { "Interactables", "Items", "Mobs" });
		}

		protected override void Start()
		{
			base.Start();

			Actions.World.Use.performed += ctx => OnUsePressed();
			Actions.World.Jump.performed += ctx => OnDodgePressed();
			Actions.World.Reload.performed += ctx => OnReloadPressed();
			Actions.World.Throw.performed += ctx => OnThrowPressed();
			Actions.World.Drop.performed += ctx => OnDropPressed();

			Actions.World.Fire.performed += ctx => OnTriggerInput(true);
			Actions.World.Fire.canceled += ctx => OnTriggerInput(false);

			// Actions.World.Sprint.performed += ctx => OnSprintInput(true);
			// Actions.World.Sprint.canceled += ctx => OnSprintInput(false);

			Actions.World.Move.canceled += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
			Actions.World.Move.performed += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
			Actions.World.Move.started += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
		}

		public override void PossessMob(Mob mob)
		{
			base.PossessMob(mob);

			CameraController.Instance.SetTrackedMob(mob);
			CameraController.Instance.followingCursor = true;

			OnPossessed?.Invoke(mob);
		}

		protected override Vector3 UpdateMovementInput()
		{
			Vector3 move = Vector3.zero;
			Vector3 movement = new Vector3(move.x, 0, move.y);

			return movement;
		}

		private void OnUsePressed()
		{
			//if (SelectedEntity is Interaction interaction)
			//	Possessed.Use(interaction);
		}

		public void OnTriggerInput(bool held) => Debug.Log("T"); // Possessed.UseItem(held);

		public void OnReloadPressed() => Debug.Log("R");
		public void OnThrowPressed() => Debug.Log("Jeter");
		private void OnDodgePressed() => Debug.Log("Sauter");
		private void OnDropPressed() => Debug.Log("Tomber");

		// private void OnSprintInput(bool sprint) =>
		// Possessed.MovementType = sprint ? MovementType.Sprinting : MovementType.Running;

		private void OnMoveInput(Vector2 inputMovement) => new Vector3(inputMovement.x, 0, inputMovement.y);

		public void OnSpecialAbilityPressed()
		{
		}

		protected override void OnUpdate(float delta)
		{
			UpdateSelectedEntitiy();
			UpdateAimPos();
		}

		public void UpdateAimPos() =>
			Possessed.AimPos = CameraController.GetWorldCursorPos();

		public void SetSelectedOutline(bool selected)
		{
		}

		public void UpdateSelectedEntitiy()
		{
			Interaction newSelected = GetSelectedEntity();
			if (newSelected != SelectedEntity)
			{
				SetSelectedOutline(false);
				SelectedEntity = newSelected;
				SetSelectedOutline(true);
			}
		}

		/// <summary>
		/// Gets the entity the possessed mob is currently selecting.
		/// Selecting means that the entity is within sector with radius equal to selectionDistance and angle equal to double selectionScope.
		/// 
		/// Won't detect entities with colliders not assigned to a layer from interactableMask.
		/// Will throw an exception if those colliders were applied to something other than entity child.
		/// </summary>
		/// <returns>The selected entity's Interaction component.</returns>
		protected virtual Interaction GetSelectedEntity()
		{
			Interaction result = null;

			Vector3 mobPos = Possessed.transform.position;
			float minDist = selectionDistance;

			Physics.OverlapSphereNonAlloc(mobPos, selectionDistance, colliderBuffer, interactableMask);
			foreach (Collider collider in colliderBuffer)
			{
				if (!collider)
					continue;

				Vector3 entityPos = collider.ClosestPoint(mobPos);
				float distance = Utils.HorizontalDistance(mobPos, entityPos);
				if ((Vector3.Angle((entityPos - mobPos), Possessed.transform.forward) > selectionScope)
					|| (distance >= minDist))
					continue;

				if (!(collider.GetComponentInParent<Interaction>() is Interaction interactable)
					|| !interactable.Selectable
					|| !interactable.CanBeUsedBy(Possessed))
					continue;

				minDist = distance;
				result = interactable;
			}

			return result;
		}
	}
}