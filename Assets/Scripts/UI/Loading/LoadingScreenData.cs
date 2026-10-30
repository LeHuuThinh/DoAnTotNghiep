using UnityEngine;
using System.Collections.Generic;

namespace InfiniteLabyrinth.UI
{
  [System.Serializable]
  public class LoadingTip
  {
    [Tooltip("Tip category, e.g., COMBAT, EXPLORATION, BOSS TACTICS")]
    public string category = "TIPS";

    [TextArea(2, 4)]
    [Tooltip("The tip content in English")]
    public string content;

    public LoadingTip() { }

    public LoadingTip(string category, string content)
    {
      this.category = category;
      this.content = content;
    }
  }

  [System.Serializable]
  public class LoadingLocationInfo
  {
    [Tooltip("Exact Scene name, e.g., DungeonScene or LandScene")]
    public string sceneName;

    [Tooltip("Main title format, e.g., 'INFINITE LABYRINTH - FLOOR {0}' or 'HAVEN VILLAGE'")]
    public string titleFormat;

    [Tooltip("Subtitle or flavour text, e.g., 'The Forgotten Crypt' or 'Sanctuary of the Wandering Knight'")]
    public string subtitle;

    public LoadingLocationInfo() { }

    public LoadingLocationInfo(string sceneName, string titleFormat, string subtitle)
    {
      this.sceneName = sceneName;
      this.titleFormat = titleFormat;
      this.subtitle = subtitle;
    }
  }

  /// <summary>
  /// ScriptableObject storing tips, background artworks, and location titles for the Loading Screen.
  /// Fully localized in English by default.
  /// </summary>
  [CreateAssetMenu(fileName = "LoadingScreenData", menuName = "Infinite Labyrinth/UI/Loading Screen Data")]
  public class LoadingScreenData : ScriptableObject
  {
    [Header("=== DUNGEON BACKGROUND ARTWORKS ===")]
    [Tooltip("Dungeon renders / screenshots (Texture2D) for full-screen loading background")]
    public Texture2D[] dungeonBackgroundTextures;

    [Tooltip("Alternative dungeon sprites if using Sprite (2D and UI) assets")]
    public Sprite[] dungeonBackgroundSprites;

    [Header("=== VILLAGE / HUB BACKGROUND ARTWORKS ===")]
    [Tooltip("Village renders / screenshots (Texture2D) for full-screen loading background")]
    public Texture2D[] villageBackgroundTextures;

    [Tooltip("Alternative village sprites if using Sprite (2D and UI) assets")]
    public Sprite[] villageBackgroundSprites;

    [Header("=== GAMEPLAY TIPS (ENGLISH) ===")]
    [Tooltip("Collection of gameplay hints displayed randomly above the progress bar")]
    public List<LoadingTip> tips = new List<LoadingTip>();

    [Header("=== LOCATION & FLOOR HEADERS ===")]
    [Tooltip("Location titles and subtitles mapped by scene name")]
    public List<LoadingLocationInfo> locations = new List<LoadingLocationInfo>();

    /// <summary>
    /// Returns a random tip from the pool, or a default fallback if empty.
    /// </summary>
    public LoadingTip GetRandomTip()
    {
      if (tips != null && tips.Count > 0)
      {
        int index = Random.Range(0, tips.Count);
        return tips[index];
      }

      return new LoadingTip("SURVIVAL", "Keep your weapon ready and mind your stamina in battle.");
    }

    /// <summary>
    /// Gets a random background texture for the specified target scene.
    /// Supports both Texture2D and Sprite assets.
    /// </summary>
    public Texture GetRandomBackground(string sceneName)
    {
      bool isVillage = !string.IsNullOrEmpty(sceneName) && sceneName.ToLower().Contains("land");

      if (isVillage)
      {
        Texture tex = PickRandomTexture(villageBackgroundTextures, villageBackgroundSprites);
        if (tex != null) return tex;
      }

      // Default or Dungeon scene
      Texture dungeonTex = PickRandomTexture(dungeonBackgroundTextures, dungeonBackgroundSprites);
      if (dungeonTex != null) return dungeonTex;

      // Fallback to village if dungeon is empty
      return PickRandomTexture(villageBackgroundTextures, villageBackgroundSprites);
    }

    private Texture PickRandomTexture(Texture2D[] textures, Sprite[] sprites)
    {
      int texCount = textures != null ? textures.Length : 0;
      int sprCount = sprites != null ? sprites.Length : 0;
      int total = texCount + sprCount;

      if (total == 0) return null;

      int pick = Random.Range(0, total);
      if (pick < texCount && textures[pick] != null)
      {
        return textures[pick];
      }
      else
      {
        int sprIndex = pick - texCount;
        if (sprIndex >= 0 && sprIndex < sprCount && sprites[sprIndex] != null)
        {
          return sprites[sprIndex].texture;
        }
      }

      return null;
    }

    /// <summary>
    /// Formats the location title and subtitle for the loading header.
    /// </summary>
    public void GetLocationHeader(string sceneName, int floorNumber, out string title, out string subtitle)
    {
      title = "LOADING REALM...";
      subtitle = "Please wait while the world takes shape";

      if (string.IsNullOrEmpty(sceneName)) return;

      if (locations != null)
      {
        foreach (var loc in locations)
        {
          if (loc.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
          {
            try
            {
              title = string.Format(loc.titleFormat, floorNumber);
            }
            catch
            {
              title = loc.titleFormat;
            }
            subtitle = loc.subtitle;
            return;
          }
        }
      }

      // Default fallbacks if not matched in list
      if (sceneName.ToLower().Contains("dungeon"))
      {
        title = $"INFINITE LABYRINTH - FLOOR {floorNumber}";
        subtitle = "The Forgotten Crypt";
      }
      else if (sceneName.ToLower().Contains("land"))
      {
        title = "HAVEN VILLAGE";
        subtitle = "Sanctuary of the Wandering Knight";
      }
    }
  }
}
