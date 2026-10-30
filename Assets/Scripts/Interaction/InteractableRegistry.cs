using System.Collections.Generic;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Sổ bộ lưu trữ danh sách các đối tượng tương tác đang hoạt động trong Scene.
  /// Giúp Player quét tìm đối tượng nhanh chóng mà không cần OverlapSphere hay Raycast liên tục.
  /// </summary>
  public static class InteractableRegistry
  {
    private static readonly List<IInteractable> activeInteractables = new List<IInteractable>();

    public static IReadOnlyList<IInteractable> ActiveInteractables => activeInteractables;

    public static void Register(IInteractable interactable)
    {
      if (interactable != null && !activeInteractables.Contains(interactable))
      {
        activeInteractables.Add(interactable);
      }
    }

    public static void Unregister(IInteractable interactable)
    {
      if (interactable != null)
      {
        activeInteractables.Remove(interactable);
      }
    }

    public static void Clear()
    {
      activeInteractables.Clear();
    }
  }
}
