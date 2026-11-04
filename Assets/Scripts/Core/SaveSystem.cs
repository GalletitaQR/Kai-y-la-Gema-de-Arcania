using UnityEngine;

/// <summary>Small PlayerPrefs wrapper: where to continue, whether the prologue was seen, staff ownership.</summary>
public static class SaveSystem
{
    public const string FirstLevelScene = "SampleScene";

    private const string SavedSceneKey = "SavedScene";
    private const string PrologueSeenKey = "PrologueSeen";
    private const string HasStaffKey = "HasStaff";
    private const string GemFragmentsKey = "GemFragments";

    public static bool HasSave => PlayerPrefs.HasKey(SavedSceneKey);
    public static string SavedScene => PlayerPrefs.GetString(SavedSceneKey, FirstLevelScene);

    public static bool PrologueSeen
    {
        get => PlayerPrefs.GetInt(PrologueSeenKey, 0) == 1;
        set => PlayerPrefs.SetInt(PrologueSeenKey, value ? 1 : 0);
    }

    public static bool HasStaff
    {
        get => PlayerPrefs.GetInt(HasStaffKey, 0) == 1;
        set => PlayerPrefs.SetInt(HasStaffKey, value ? 1 : 0);
    }

    public static int GemFragments
    {
        get => PlayerPrefs.GetInt(GemFragmentsKey, 0);
        set => PlayerPrefs.SetInt(GemFragmentsKey, value);
    }

    public static void SaveScene(string sceneName)
    {
        PlayerPrefs.SetString(SavedSceneKey, sceneName);
        PlayerPrefs.Save();
    }

    /// <summary>Clears all progress (used by "Jugar").</summary>
    public static void NewGame()
    {
        PlayerPrefs.DeleteKey(SavedSceneKey);
        PrologueSeen = false;
        HasStaff = false;
        GemFragments = 0;
        PlayerPrefs.Save();
    }
}
