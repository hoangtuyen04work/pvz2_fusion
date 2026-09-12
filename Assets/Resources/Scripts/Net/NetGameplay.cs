using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tự gắn bộ đồng bộ vào GameScene khi đang chơi mạng.
/// </summary>
public static class NetGameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;
        if (!NetSession.IsOnline) return;
        if (Object.FindAnyObjectByType<NetGameplay>() != null) return;
        new GameObject("Net Gameplay", typeof(NetGameplay));
    }
}

/// <summary>
/// Bộ đồng bộ trong màn chơi.
/// Máy chủ chạy toàn bộ logic rồi phát kết quả; máy khách gửi yêu cầu và vẽ lại.
/// </summary>
public class NetGameplay : MonoBehaviour
{
    private static NetGameplay instance;
    public static NetGameplay Instance { get { return instance; } }

    private const float SyncInterval = 0.1f;    //Mười lần mỗi giây
    private const float BrainInterval = 1f;     //Nhịp hồi não của phe zombie

    private readonly Dictionary<string, PlantGrid> grids = new Dictionary<string, PlantGrid>();
    private readonly Dictionary<int, Zombie> zombies = new Dictionary<int, Zombie>();
    private readonly Dictionary<int, SunBase> suns = new Dictionary<int, SunBase>();

    private int nextNetId = 1;
    private float nextSyncTime;
    private float nextBrainTime;
    private bool gridsReady;
    private bool ended;

    //Tài nguyên của phe zombie trong chế độ đối kháng
    public int Brains { get; private set; }
    public const int BrainMax = 500;
    public const int BrainPerTick = 10;
    public const int BrainStart = 100;

    //Độ dài một trận đối kháng. Phe cây trụ được hết giờ là thắng.
    public const float PvpMatchSeconds = 240f;
    private float pvpDeadline;
    public int PvpSecondsLeft { get; private set; }

    private Text hudText;
    private Text alertText;
    private float alertUntil;

    private SunNumber sunNumber;
    private ZombieManagement zombieManagement;

    #region Vòng đời

    private void Awake()
    {
        instance = this;
        Brains = BrainStart;
        PvpSecondsLeft = Mathf.CeilToInt(PvpMatchSeconds);
    }

    private void Start()
    {
        NetManager manager = NetManager.Instance;
        manager.OnMessage += HandleMessage;
        manager.OnPeerLost += HandlePeerLost;

        BuildHud();
        HidePlantUiForZombieSide();

        //Phe zombie cần thanh điều khiển riêng để thả quân
        if (NetSession.ControlsZombies && Object.FindAnyObjectByType<ZombieCommanderUI>() == null)
            new GameObject("Zombie Commander", typeof(ZombieCommanderUI));

        nextSyncTime = Time.time + SyncInterval;
        nextBrainTime = Time.time + BrainInterval;
        pvpDeadline = Time.time + PvpMatchSeconds;

        //Xử lý nốt các gói tin đã tới trong lúc màn chơi còn đang nạp
        foreach (NetMessage waiting in manager.TakePending()) HandleMessage(waiting);
    }

    private void OnDestroy()
    {
        if (NetManager.Exists)
        {
            NetManager.Instance.OnMessage -= HandleMessage;
            NetManager.Instance.OnPeerLost -= HandlePeerLost;
        }
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (NetSession.IsAuthority && !ended)
        {
            if (Time.time >= nextSyncTime)
            {
                nextSyncTime = Time.time + SyncInterval;
                SendZombieBatch();
            }

            if (NetSession.Mode == NetGameMode.Pvp && Time.time >= nextBrainTime)
            {
                nextBrainTime = Time.time + BrainInterval;
                TickPvpMatch();
            }
        }

        UpdateHud();
    }

    #endregion

    #region Tra cứu đối tượng

    private void BuildGridTable()
    {
        grids.Clear();
        PlantGrid[] all = Object.FindObjectsByType<PlantGrid>(FindObjectsInactive.Include);
        foreach (PlantGrid grid in all)
        {
            //Tên ô đất trong prefab có dạng "Plant-cột-hàng" nên dùng luôn làm khoá mạng
            if (!grids.ContainsKey(grid.name)) grids.Add(grid.name, grid);
        }
        gridsReady = grids.Count > 0;
    }

    private PlantGrid FindGrid(string gridName)
    {
        if (!gridsReady) BuildGridTable();
        PlantGrid grid;
        if (grids.TryGetValue(gridName, out grid) && grid != null) return grid;

        //Ô đất có thể được nạp muộn, thử dựng lại bảng một lần nữa
        BuildGridTable();
        return grids.TryGetValue(gridName, out grid) ? grid : null;
    }

    private SunNumber Sun
    {
        get
        {
            if (sunNumber == null)
            {
                GameObject text = GameObject.Find("Sun Text");
                if (text != null) sunNumber = text.GetComponent<SunNumber>();
            }
            return sunNumber;
        }
    }

    private ZombieManagement Zombies
    {
        get
        {
            if (zombieManagement == null)
            {
                GameObject holder = GameObject.Find("Zombie Management");
                if (holder != null) zombieManagement = holder.GetComponent<ZombieManagement>();
            }
            return zombieManagement;
        }
    }

    private Card FindCard(string plantName)
    {
        if (Sun == null) return null;
        List<Card> cards = Sun.Cards;
        if (cards == null) return null;
        foreach (Card card in cards)
        {
            if (card != null && card.plantName == plantName) return card;
        }
        return null;
    }

    #endregion

    #region Trồng cây

    /// <summary>
    /// Xin đặt cây xuống một ô. Chơi đơn thì đặt luôn, chơi mạng thì đi qua máy chủ.
    /// Trả về true nếu thao tác được chấp nhận ở phía người chơi (để giao diện thu cây chờ trồng lại).
    /// </summary>
    public static bool RequestPlace(string gridName, string plantName)
    {
        if (!NetSession.IsOnline || instance == null) return false;
        if (!NetSession.ControlsPlants) return false;

        if (NetSession.IsAuthority)
            return instance.TryPlaceAsHost(gridName, plantName);

        //Máy khách chỉ gửi yêu cầu, chờ máy chủ xác nhận
        NetManager.Broadcast(NetMessage.Of(NetMsg.PlantReq).Str(gridName, plantName));
        return true;
    }

    private bool TryPlaceAsHost(string gridName, string plantName)
    {
        PlantGrid grid = FindGrid(gridName);
        if (grid == null) return false;

        Card card = FindCard(plantName);
        if (card == null) return false;
        if (card.IsCooling) return false;
        if (Sun == null || Sun.Current < card.sunNeeded) return false;

        if (!grid.placePlant(plantName)) return false;

        ChargeAndCool(plantName);
        NetManager.Broadcast(NetMessage.Of(NetMsg.PlantAck).Str(gridName, plantName));
        return true;
    }

    /// <summary>Trừ nắng (chỉ máy chủ) và cho thẻ cây vào hồi chiêu (cả hai máy).</summary>
    private void ChargeAndCool(string plantName)
    {
        Card card = FindCard(plantName);
        if (card == null) return;

        if (NetSession.IsAuthority && Sun != null)
        {
            Sun.subSunAuthoritative(card.sunNeeded);
            NetManager.Broadcast(NetMessage.Of(NetMsg.SunSet).Int(Sun.Current));
        }

        card.cooling();
    }

    /// <summary>Xin đào cây khỏi ô.</summary>
    public static bool RequestShovel(string gridName)
    {
        if (!NetSession.IsOnline || instance == null) return false;
        if (!NetSession.ControlsPlants) return false;

        if (NetSession.IsAuthority) return false;   //Máy chủ tự đào tại chỗ như thường

        NetManager.Broadcast(NetMessage.Of(NetMsg.ShovelReq).Str(gridName));
        return true;
    }

    /// <summary>Máy chủ báo cho máy khách biết một cây vừa biến mất.</summary>
    public static void NotifyPlantRemoved(PlantGrid grid, string reason)
    {
        if (!NetSession.IsOnline || grid == null) return;
        if (!NetSession.IsAuthority) return;
        NetManager.Broadcast(NetMessage.Of(NetMsg.PlantRemove).Str(grid.name, reason));
    }

    #endregion

    #region Nắng

    /// <summary>Ghi danh một mặt trời vừa sinh ra. Máy chủ cấp id và phát cho máy khách.</summary>
    public static void RegisterSun(SunBase sun)
    {
        if (!NetSession.IsOnline || instance == null || sun == null) return;

        if (sun.netId != 0)
        {
            //Máy khách: đối tượng do gói tin tạo ra, đã có sẵn id
            instance.suns[sun.netId] = sun;
            return;
        }

        if (!NetSession.IsAuthority) return;

        sun.netId = instance.nextNetId++;
        instance.suns[sun.netId] = sun;

        string key = sun.gameObject.name.Replace("(Clone)", "").Trim();
        Vector3 position = sun.transform.position;

        NetManager.Broadcast(NetMessage.Of(NetMsg.SunSpawn)
            .Str(key)
            .Int(sun.netId, sun.sunNumber)
            .Flt(position.x, position.y, sun.netParam));
    }

    /// <summary>Người chơi bấm vào một mặt trời.</summary>
    public static void RequestSunPickup(SunBase sun)
    {
        if (sun == null) return;

        if (!NetSession.IsOnline)
        {
            sun.bePickedUp();
            return;
        }

        if (!NetSession.ControlsPlants) return;

        if (NetSession.IsAuthority)
        {
            sun.bePickedUp();
            NetManager.Broadcast(NetMessage.Of(NetMsg.SunPicked).Int(sun.netId));
        }
        else
        {
            NetManager.Broadcast(NetMessage.Of(NetMsg.SunPickReq).Int(sun.netId));
        }
    }

    /// <summary>Máy chủ phát tổng số nắng mỗi khi thay đổi.</summary>
    public static void NotifySunTotal(int total)
    {
        if (!NetSession.IsOnline || !NetSession.IsAuthority) return;
        NetManager.Broadcast(NetMessage.Of(NetMsg.SunSet).Int(total));
    }

    #endregion

    #region Zombie

    /// <summary>Máy chủ ghi danh một zombie vừa sinh ra và phát cho máy khách.</summary>
    public static void RegisterZombie(Zombie zombie, string prefabName, int row,
        float posX, float posY, float speedScale, float sleepTime, int priorNetId)
    {
        if (!NetSession.IsOnline || instance == null || zombie == null) return;
        if (!NetSession.IsAuthority) return;

        NetZombieView view = zombie.gameObject.AddComponent<NetZombieView>();
        view.netId = instance.nextNetId++;
        instance.zombies[view.netId] = zombie;

        NetMessage message = NetMessage.Of(NetMsg.ZombieSpawn)
            .Str(prefabName)
            .Int(view.netId, row, Mathf.RoundToInt(sleepTime * 1000f))
            .Flt(posX, posY, speedScale);
        message.p = priorNetId;
        NetManager.Broadcast(message);
    }

    /// <summary>Id mạng của một zombie, 0 nếu chưa có.</summary>
    public static int NetIdOf(Zombie zombie)
    {
        if (zombie == null) return 0;
        NetZombieView view = zombie.GetComponent<NetZombieView>();
        return view != null ? view.netId : 0;
    }

    /// <summary>Máy chủ báo một zombie đã chết.</summary>
    public static void NotifyZombieDead(Zombie zombie, bool squashed)
    {
        if (!NetSession.IsOnline || !NetSession.IsAuthority) return;
        int id = NetIdOf(zombie);
        if (id == 0) return;
        NetManager.Broadcast(NetMessage.Of(NetMsg.ZombieDie).Int(id).Bool(squashed));
        if (instance != null) instance.zombies.Remove(id);
    }

    /// <summary>Gói toàn bộ vị trí và máu zombie vào một gói tin duy nhất.</summary>
    private void SendZombieBatch()
    {
        if (zombies.Count == 0) return;

        StringBuilder builder = new StringBuilder(zombies.Count * 24);
        List<int> dead = null;

        foreach (KeyValuePair<int, Zombie> pair in zombies)
        {
            Zombie zombie = pair.Value;
            if (zombie == null)
            {
                if (dead == null) dead = new List<int>();
                dead.Add(pair.Key);
                continue;
            }

            Vector3 position = zombie.transform.position;
            if (builder.Length > 0) builder.Append(';');
            builder.Append(pair.Key).Append(':')
                .Append(position.x.ToString("F3", CultureInfo.InvariantCulture)).Append(':')
                .Append(position.y.ToString("F3", CultureInfo.InvariantCulture)).Append(':')
                .Append(zombie.bloodVolume);
        }

        if (dead != null)
        {
            foreach (int id in dead) zombies.Remove(id);
        }

        if (builder.Length == 0) return;
        NetManager.Broadcast(NetMessage.Of(NetMsg.ZombieBatch).Str(builder.ToString()));
    }

    private void ApplyZombieBatch(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return;

        string[] entries = payload.Split(';');
        for (int e = 0; e < entries.Length; e++)
        {
            string[] parts = entries[e].Split(':');
            if (parts.Length < 4) continue;

            int id;
            float px, py;
            int hp;
            if (!int.TryParse(parts[0], out id)) continue;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out px)) continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out py)) continue;
            if (!int.TryParse(parts[3], out hp)) continue;

            Zombie zombie;
            if (!zombies.TryGetValue(id, out zombie) || zombie == null) continue;

            zombie.bloodVolume = hp;

            //Gọi lại với sát thương 0 để lớp con cập nhật hình dáng theo mức máu
            //(rơi mũ chóp, thủng xô, tạo khiên băng...). Máu không đổi vì lớp cha đã chặn.
            zombie.beAttacked(0);

            NetZombieView view = zombie.GetComponent<NetZombieView>();
            if (view != null) view.SetTarget(new Vector3(px, py, zombie.transform.position.z));
        }
    }

    #endregion

    #region Chế độ đối kháng

    /// <summary>Phe zombie xin thả một con zombie xuống một hàng.</summary>
    public static void RequestZombieDrop(string zombieName, int row)
    {
        if (!NetSession.IsOnline || !NetSession.ControlsZombies) return;
        NetManager.Broadcast(NetMessage.Of(NetMsg.ZombieCmdReq).Str(zombieName).Int(0, row));
    }

    //Mỗi giây: hồi não cho phe zombie, đếm ngược đồng hồ trận đấu, rồi báo cho máy khách
    private void TickPvpMatch()
    {
        Brains = Mathf.Clamp(Brains + BrainPerTick, 0, BrainMax);
        PvpSecondsLeft = Mathf.Max(0, Mathf.CeilToInt(pvpDeadline - Time.time));

        NetManager.Broadcast(NetMessage.Of(NetMsg.BrainSet).Int(Brains, PvpSecondsLeft));

        //Hết giờ mà não vẫn còn nguyên vẹn thì phe cây thắng
        if (PvpSecondsLeft > 0 || ended) return;

        GameObject management = GameObject.Find("Game Management");
        if (management == null) return;
        GameManagement game = management.GetComponent<GameManagement>();
        if (game != null) game.win();
    }

    private void SpendBrains(int amount)
    {
        Brains = Mathf.Clamp(Brains - amount, 0, BrainMax);
        NetManager.Broadcast(NetMessage.Of(NetMsg.BrainSet).Int(Brains, PvpSecondsLeft));
    }

    private void HandleZombieCommand(NetMessage message)
    {
        if (!NetSession.IsAuthority || NetSession.Mode != NetGameMode.Pvp) return;
        if (ended) return;

        string zombieName = message.s;
        int row = message.j;
        int cost = ZombieRoster.CostOf(zombieName);

        if (cost <= 0)
        {
            NetManager.Broadcast(NetMessage.Of(NetMsg.CmdReply).Str(zombieName, "Không có loại zombie này").Bool(false));
            return;
        }

        if (Brains < cost)
        {
            NetManager.Broadcast(NetMessage.Of(NetMsg.CmdReply).Str(zombieName, "Không đủ não").Bool(false));
            return;
        }

        if (Zombies == null)
        {
            NetManager.Broadcast(NetMessage.Of(NetMsg.CmdReply).Str(zombieName, "Màn chưa sẵn sàng").Bool(false));
            return;
        }

        GameObject spawned = Zombies.spawnCommandedZombie(zombieName, row);
        if (spawned == null)
        {
            NetManager.Broadcast(NetMessage.Of(NetMsg.CmdReply).Str(zombieName, "Không thả được").Bool(false));
            return;
        }

        SpendBrains(cost);
        NetManager.Broadcast(NetMessage.Of(NetMsg.CmdReply).Str(zombieName, "").Bool(true));
    }

    #endregion

    #region Kết thúc màn

    /// <summary>Máy chủ báo kết thúc màn. plantsLost = true nghĩa là zombie đã ăn được não.</summary>
    public static void NotifyGameEnd(bool plantsLost)
    {
        if (!NetSession.IsOnline || !NetSession.IsAuthority) return;
        if (instance != null)
        {
            if (instance.ended) return;
            instance.ended = true;
        }
        NetManager.Broadcast(NetMessage.Of(NetMsg.GameEnd).Bool(plantsLost));
    }

    /// <summary>Máy chủ đồng bộ thanh tiến trình và phụ đề đợt lớn.</summary>
    public static void NotifyProgress(float value, string captionKind)
    {
        if (!NetSession.IsOnline || !NetSession.IsAuthority) return;
        NetMessage message = NetMessage.Of(NetMsg.Caption).Str(captionKind);
        message.x = value;
        NetManager.Broadcast(message);
    }

    #endregion

    #region Nhận gói tin

    private void HandleMessage(NetMessage message)
    {
        switch (message.t)
        {
            case NetMsg.PlantReq:
                if (NetSession.IsAuthority) TryPlaceAsHost(message.s, message.u);
                break;

            case NetMsg.PlantAck:
                ApplyRemotePlace(message.s, message.u);
                break;

            case NetMsg.ShovelReq:
                if (NetSession.IsAuthority) ApplyShovelAsHost(message.s);
                break;

            case NetMsg.PlantRemove:
                ApplyPlantRemoved(message.s, message.u);
                break;

            case NetMsg.SunSpawn:
                ApplySunSpawn(message);
                break;

            case NetMsg.SunPickReq:
                if (NetSession.IsAuthority) ApplySunPickRequest(message.i);
                break;

            case NetMsg.SunPicked:
                ApplySunPicked(message.i);
                break;

            case NetMsg.SunSet:
                if (Sun != null) Sun.setSun(message.i);
                break;

            case NetMsg.ZombieSpawn:
                ApplyZombieSpawn(message);
                break;

            case NetMsg.ZombieBatch:
                ApplyZombieBatch(message.s);
                break;

            case NetMsg.ZombieDie:
                ApplyZombieDeath(message.i, message.b);
                break;

            case NetMsg.ZombieCmdReq:
                HandleZombieCommand(message);
                break;

            case NetMsg.BrainSet:
                Brains = message.i;
                PvpSecondsLeft = message.j;
                break;

            case NetMsg.CmdReply:
                if (ZombieCommanderUI.Instance != null)
                    ZombieCommanderUI.Instance.OnCommandResult(message.s, message.b, message.u);
                break;

            case NetMsg.Caption:
                ApplyProgress(message);
                break;

            case NetMsg.GameEnd:
                ApplyGameEnd(message.b);
                break;
        }
    }

    private void ApplyRemotePlace(string gridName, string plantName)
    {
        PlantGrid grid = FindGrid(gridName);
        if (grid == null) return;
        if (!grid.placePlant(plantName)) return;
        ChargeAndCool(plantName);
    }

    private void ApplyShovelAsHost(string gridName)
    {
        PlantGrid grid = FindGrid(gridName);
        if (grid == null) return;
        grid.removePlantByShovel();
    }

    private void ApplyPlantRemoved(string gridName, string reason)
    {
        if (NetSession.IsAuthority) return;
        PlantGrid grid = FindGrid(gridName);
        if (grid == null) return;
        grid.removePlantFromNetwork(reason);
    }

    private void ApplySunSpawn(NetMessage message)
    {
        if (NetSession.IsAuthority) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Sun/" + message.s);
        if (prefab == null) return;

        GameObject parent = GameObject.Find("Sun Management");
        GameObject created = Instantiate(prefab,
            new Vector3(message.x, message.y, 0f),
            Quaternion.identity,
            parent != null ? parent.transform : null);
        created.name = message.s;

        SunBase sun = created.GetComponent<SunBase>();
        if (sun == null) return;

        //Đặt trước khi Start chạy để mặt trời rơi đúng chỗ như bên máy chủ
        sun.netId = message.i;
        sun.sunNumber = message.j;
        sun.netParam = message.z;
        sun.netParamSet = true;
        suns[sun.netId] = sun;
    }

    private void ApplySunPickRequest(int netId)
    {
        SunBase sun;
        if (!suns.TryGetValue(netId, out sun) || sun == null) return;
        sun.bePickedUp();
        NetManager.Broadcast(NetMessage.Of(NetMsg.SunPicked).Int(netId));
    }

    private void ApplySunPicked(int netId)
    {
        if (NetSession.IsAuthority) return;
        SunBase sun;
        if (!suns.TryGetValue(netId, out sun) || sun == null) return;
        sun.bePickedUp();
    }

    private void ApplyZombieSpawn(NetMessage message)
    {
        if (NetSession.IsAuthority) return;
        if (Zombies == null) return;

        Zombie prior = null;
        if (message.p > 0)
        {
            Zombie found;
            if (zombies.TryGetValue(message.p, out found)) prior = found;
        }

        GameObject created = Zombies.spawnZombieFromNetwork(
            message.s, message.j, message.x, message.y, message.z, message.k / 1000f, prior);
        if (created == null) return;

        Zombie zombie = created.GetComponent<Zombie>();
        NetZombieView view = created.AddComponent<NetZombieView>();
        view.netId = message.i;
        view.SetTarget(created.transform.position);
        zombies[message.i] = zombie;
    }

    private void ApplyZombieDeath(int netId, bool squashed)
    {
        if (NetSession.IsAuthority) return;

        Zombie zombie;
        if (!zombies.TryGetValue(netId, out zombie)) return;
        zombies.Remove(netId);
        if (zombie == null) return;

        zombie.applyNetworkDeath(squashed);
    }

    private void ApplyProgress(NetMessage message)
    {
        if (NetSession.IsAuthority) return;

        GameObject meter = GameObject.Find("FlagMeter-Slider");
        if (meter != null)
        {
            DecreasingSlider slider = meter.GetComponent<DecreasingSlider>();
            if (slider != null) slider.setValue(message.x);
        }

        if (string.IsNullOrEmpty(message.s)) return;

        //Lấy qua Zombie Management vì đối tượng phụ đề có lúc đang tắt
        Caption caption = Zombies != null ? Zombies.caption : null;
        if (caption == null) return;
        if (message.s == "final") caption.showFinalWave();
        else caption.showWave();
    }

    private void ApplyGameEnd(bool plantsLost)
    {
        if (NetSession.IsAuthority) return;
        if (ended) return;
        ended = true;

        GameObject management = GameObject.Find("Game Management");
        if (management == null) return;
        GameManagement game = management.GetComponent<GameManagement>();
        if (game == null) return;

        if (plantsLost) game.gameOver();
        else game.win();
    }

    private void HandlePeerLost(string reason)
    {
        ShowAlert(string.IsNullOrEmpty(reason) ? "Mất kết nối với đối phương" : reason, 600f);
    }

    #endregion

    #region Giao diện phụ

    private void HidePlantUiForZombieSide()
    {
        if (NetSession.ControlsPlants) return;

        //Phe zombie không trồng cây nên giấu dãy thẻ và cái xẻng đi
        string[] hidden = { "Seed Bank", "Shovel Bank" };
        foreach (string name in hidden)
        {
            GameObject target = GameObject.Find(name);
            if (target != null) target.SetActive(false);
        }
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("NetHudCanvas",
            typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.Load<Font>("Fonts/Baloo2");

        hudText = CreateHudText(canvasObject.transform, font, "", 18,
            TextAnchor.UpperRight, new Color(0.85f, 1f, 0.7f, 0.9f),
            0.60f, 0.925f, 0.995f, 0.995f);

        alertText = CreateHudText(canvasObject.transform, font, "", 22,
            TextAnchor.UpperCenter, new Color(1f, 0.6f, 0.35f),
            0.15f, 0.845f, 0.85f, 0.925f);
    }

    private Text CreateHudText(Transform parent, Font font, string value, int size,
        TextAnchor alignment, Color color, float xMin, float yMin, float xMax, float yMax)
    {
        GameObject go = new GameObject("HUD " + alignment,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void UpdateHud()
    {
        if (hudText == null) return;

        string ping = NetManager.Exists ? NetManager.Instance.PingMs + " ms" : "-";
        string line = NetSession.ModeLabel + " · " + NetSession.RoleLabel + " · " + ping;

        if (NetSession.Mode == NetGameMode.Pvp)
            line += "\nCòn lại " + (PvpSecondsLeft / 60) + ":" + (PvpSecondsLeft % 60).ToString("00");

        hudText.text = line;

        if (alertText != null && alertText.text.Length > 0 && Time.unscaledTime > alertUntil)
            alertText.text = "";
    }

    /// <summary>Hiện một dòng nhắc ngắn giữa màn hình.</summary>
    public void ShowAlert(string message, float seconds = 2.5f)
    {
        if (alertText == null || string.IsNullOrEmpty(message)) return;
        alertText.text = message;
        alertUntil = Time.unscaledTime + seconds;
    }

    #endregion
}

/// <summary>
/// Bảng giá não của từng loại zombie trong chế độ đối kháng.
/// </summary>
public static class ZombieRoster
{
    public class Entry
    {
        public string name;
        public string label;
        public int cost;
        public float cooldown;

        public Entry(string name, string label, int cost, float cooldown)
        {
            this.name = name;
            this.label = label;
            this.cost = cost;
            this.cooldown = cooldown;
        }
    }

    public static readonly Entry[] All =
    {
        new Entry("ZombieNormal",   "Zombie thường", 25,  3f),
        new Entry("ConeZombie",     "Mũ chóp",       50,  6f),
        new Entry("ChineseZombie",  "Thầy phù thuỷ", 75,  10f),
        new Entry("BucketZombie",   "Đội xô",        100, 12f),
        new Entry("Ghost",          "Bóng ma",       100, 14f),
        new Entry("SnowZombie",     "Zombie tuyết",  125, 16f),
        new Entry("BoneZombie",     "Zombie xương",  150, 20f),
        new Entry("IceBlockZombie", "Khối băng",     175, 24f),
        new Entry("YetiZombie",     "Người tuyết",   200, 30f)
    };

    public static int CostOf(string zombieName)
    {
        foreach (Entry entry in All)
        {
            if (entry.name == zombieName) return entry.cost;
        }
        return 0;
    }
}
