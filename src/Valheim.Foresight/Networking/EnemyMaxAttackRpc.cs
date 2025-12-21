using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Valheim.Foresight.Networking;

public static class EnemyMaxAttackRpc
{
    private const string RpcReq = "Foresight_ReqEnemyMaxAttack";
    private const string RpcRes = "Foresight_ResEnemyMaxAttack";

    private static CustomRPC? _req;
    private static CustomRPC? _res;

    // key = $"{prefab}|{level}"
    private static readonly Dictionary<string, float> Cache = new();
    private static readonly HashSet<string> Pending = new();

    // Серверная функция расчёта maxAttack по типу моба (префабу) и уровню
    public static System.Func<string, int, float>? ComputeOnServer;

    public static void Init()
    {
        _req = NetworkManager.Instance.AddRPC(RpcReq, OnServer_Req, OnClient_Req);
        _res = NetworkManager.Instance.AddRPC(RpcRes, OnServer_Res, OnClient_Res);
    }

    public static bool TryGet(string prefabName, int level, out float maxAttack)
    {
        var key = MakeKey(prefabName, level);
        return Cache.TryGetValue(key, out maxAttack) && maxAttack > 0f;
    }

    public static void RequestIfNeeded(string prefabName, int level)
    {
        if (_req == null)
            return;

        if (string.IsNullOrWhiteSpace(prefabName))
            return;

        if (ZRoutedRpc.instance == null)
            return;

        var key = MakeKey(prefabName, level);
        if (Pending.Contains(key) || Cache.ContainsKey(key))
            return;

        Pending.Add(key);

        var pkg = new ZPackage();
        pkg.Write(prefabName); // string [web:109]
        pkg.Write(level); // int [web:109]

        _req.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg); // server target [web:110]
    }

    private static string MakeKey(string prefabName, int level) => $"{prefabName}|{level}";

    // ---------------- client handlers ----------------
    private static IEnumerator OnClient_Req(long sender, ZPackage pkg)
    {
        yield break;
    }

    private static IEnumerator OnClient_Res(long sender, ZPackage pkg)
    {
        var prefabName = pkg.ReadString();
        var level = pkg.ReadInt();
        var max = pkg.ReadSingle();

        var key = MakeKey(prefabName, level);
        Cache[key] = max;
        Pending.Remove(key);

        yield break;
    }

    // ---------------- server handlers ----------------
    private static IEnumerator OnServer_Req(long sender, ZPackage pkg)
    {
        var prefabName = pkg.ReadString();
        var level = pkg.ReadInt();

        float maxAttack = 0f;
        if (ComputeOnServer != null)
            maxAttack = ComputeOnServer(prefabName, level);

        var res = new ZPackage();
        res.Write(prefabName);
        res.Write(level);
        res.Write(maxAttack);

        _res!.SendPackage(sender, res);
        yield break;
    }

    private static IEnumerator OnServer_Res(long sender, ZPackage pkg)
    {
        yield break;
    }
}
