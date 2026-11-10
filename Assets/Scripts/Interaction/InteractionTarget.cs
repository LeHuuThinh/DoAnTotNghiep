using System;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.Interaction
{
  public enum InteractionTargetType
  {
    Interactable,
    Pickup
  }

  /// <summary>
  /// Đại diện cho một mục tiêu có thể tương tác trong tầm (có thể là IInteractable hoặc WorldItemPickup).
  /// Chuẩn hóa dữ liệu hiển thị (Tên, Gợi ý, Phẩm cấp, Hold/Tap) cho danh sách tương tác dọc.
  /// </summary>
  public class InteractionTarget
  {
    public InteractionTargetType Type { get; }
    public IInteractable Interactable { get; }
    public WorldItemPickup Pickup { get; }

    public Transform Transform => Type == InteractionTargetType.Interactable
      ? (Interactable != null ? Interactable.InteractionTransform : null)
      : (Pickup != null ? Pickup.transform : null);

    public float Radius => Type == InteractionTargetType.Interactable
      ? (Interactable != null ? Interactable.InteractionRadius : 2.5f)
      : (Pickup != null ? Pickup.PickupRadius : 2.2f);

    public bool RequiresHold => Type == InteractionTargetType.Interactable
      ? (Interactable != null && Interactable.RequiresHold)
      : false;

    public float HoldDuration => Type == InteractionTargetType.Interactable
      ? (Interactable != null ? Interactable.HoldDuration : 0f)
      : 0f;

    public string PromptText => Type == InteractionTargetType.Interactable
      ? (Interactable != null ? Interactable.InteractionPrompt : "Tương tác")
      : (Pickup != null && Pickup.Quantity > 1 ? $"Pick up x{Pickup.Quantity}" : "Pick up");

    public string TargetName => Type == InteractionTargetType.Interactable
      ? (Interactable != null ? Interactable.TargetName : "")
      : (Pickup != null && Pickup.ItemData != null ? Pickup.ItemData.ItemName : "");

    public InteractionTarget(IInteractable interactable)
    {
      Type = InteractionTargetType.Interactable;
      Interactable = interactable;
      Pickup = null;
    }

    public InteractionTarget(WorldItemPickup pickup)
    {
      Type = InteractionTargetType.Pickup;
      Interactable = null;
      Pickup = pickup;
    }

    public bool IsValid()
    {
      if (Type == InteractionTargetType.Interactable)
      {
        return Interactable != null && Interactable.InteractionTransform != null;
      }
      else
      {
        return Pickup != null && Pickup.gameObject != null && Pickup.IsLanded;
      }
    }

    public void Execute(GameObject player)
    {
      if (Type == InteractionTargetType.Interactable)
      {
        if (Interactable != null && Interactable.CanInteract(player))
        {
          Interactable.OnInteract(player);
        }
      }
      else if (Type == InteractionTargetType.Pickup)
      {
        if (Pickup != null)
        {
          Pickup.Collect(player);
        }
      }
    }
  }
}
