namespace Valheim.Foresight.Extensions;

public static class UnityExtensions
{
    public static string GetPrefabName(this Character c) =>
        c.gameObject.name.Replace("(Clone)", "").Trim();
}
