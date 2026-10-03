using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const int GargantuarArenaLevelIndex = 6;
    private const int ThreeWorldsCampaignLevelIndex = 7;

    public Font menuFont;
    public Sprite stoneButtonNormal;
    public Sprite stoneButtonHighlighted;
    public Sprite stoneButtonPressed;
    public GameObject levelPanel;
    public GameObject helpPanel;
    public AudioSource audioSource;

    private CanvasGroup menuGroup;
    private Image fadeImage;
    private Text noticeText;
    private bool transitioning;
    private GameObject optionsPanel;
    private GameObject plantLibraryPanel;
    private GameObject zombieLibraryPanel;
    private int selectedLevel = -1;
    private Button playLevelButton;
    private Text selectedLevelText;
    private readonly List<Image> levelCardFrames = new List<Image>();
    private Material menuHoverMaterial;
    private float menuHoverTarget;
    private float menuHoverAmount;

    private void Update()
    {
        if (menuHoverMaterial == null) return;
        menuHoverAmount = Mathf.Lerp(menuHoverAmount, menuHoverTarget, 18f * Time.unscaledDeltaTime);
        menuHoverMaterial.SetFloat("_HoverAmount", menuHoverAmount);
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (menuFont == null)
            menuFont = Resources.Load<Font>("Fonts/Baloo2");

        BuildMenu();
        StartCoroutine(AnimateEntrance());
    }

    private void BuildMenu()
    {
        if (GameObject.Find("MainMenuCanvas") != null)
            return;

        EnsureMenuCamera();

        var canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        menuGroup = canvasObject.GetComponent<CanvasGroup>();
        menuGroup.alpha = 0f;

        EnsureEventSystem();

        var frameObject = new GameObject("Khung Menu 4x3", typeof(RectTransform), typeof(AspectRatioFitter));
        frameObject.transform.SetParent(canvasObject.transform, false);
        Stretch(frameObject.GetComponent<RectTransform>());
        var frameFitter = frameObject.GetComponent<AspectRatioFitter>();
        frameFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        frameFitter.aspectRatio = 4f / 3f;
        Transform menuFrame = frameObject.transform;

        var background = CreateImage("Nền Menu", menuFrame, Resources.Load<Sprite>("Sprites/UI/MainMenu/main_menu_vi"));
        Stretch(background.rectTransform);
        background.raycastTarget = false;
        var hoverShader = Resources.Load<Shader>("Shaders/UIMenuTextHover");
        if (hoverShader != null)
        {
            menuHoverMaterial = new Material(hoverShader);
            background.material = menuHoverMaterial;
        }

        // Các hitbox bám theo đúng vị trí các phiến đá trên ảnh nền 4:3.
        CreateHotspot(menuFrame, "Phiêu lưu", 0.500f, 0.656f, 0.891f, 0.843f, new Vector4(0.520f, 0.710f, 0.865f, 0.825f), StartAdventure);
        CreateHotspot(menuFrame, "Trò chơi nhỏ", 0.503f, 0.497f, 0.880f, 0.690f, new Vector4(0.520f, 0.535f, 0.850f, 0.655f), () => ShowNotice("Chế độ Trò chơi nhỏ sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Giải đố", 0.512f, 0.385f, 0.862f, 0.548f, new Vector4(0.545f, 0.420f, 0.835f, 0.515f), () => ShowNotice("Chế độ Giải đố sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Sinh tồn", 0.514f, 0.287f, 0.839f, 0.435f, new Vector4(0.540f, 0.310f, 0.805f, 0.405f), EndlessMenuOverlay.Show);
        CreateHotspot(menuFrame, "Cửa hàng", 0.341f, 0.059f, 0.445f, 0.144f, new Vector4(0.350f, 0.075f, 0.435f, 0.130f), () => ShowNotice("Cửa hàng hiện đang đóng cửa."));
        // Tấm phủ đá che chữ "TÙY CHỌN" cũ trên ảnh nền (không dùng nữa)
        var optionCover = CreateImage("Tấm phủ đá TÙY CHỌN", menuFrame, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        optionCover.type = Image.Type.Sliced;
        optionCover.color = new Color(0.32f, 0.30f, 0.26f, 0.97f);
        SetAnchors(optionCover.rectTransform, 0.578f, 0.108f, 0.690f, 0.200f);
        optionCover.raycastTarget = false;

        // Nút ZOMBIE — đặt ở vùng cuốn sách Suburban Almanac (bên trái cuốn sách)
        var zombieStonePatch = CreateImage("Tấm phủ đá Zombie", menuFrame, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        zombieStonePatch.type = Image.Type.Sliced;
        zombieStonePatch.color = new Color(0.36f, 0.18f, 0.16f, 0.97f);
        SetAnchors(zombieStonePatch.rectTransform, 0.405f, 0.032f, 0.495f, 0.118f);
        zombieStonePatch.raycastTarget = false;

        CreateHotspot(menuFrame, "Danh sách zombie", 0.400f, 0.025f, 0.500f, 0.145f, new Vector4(0.410f, 0.040f, 0.492f, 0.112f), ShowZombieLibrary);
        var zombieLibraryLabel = CreateText("Nhãn danh sách zombie", menuFrame, "ZOMBIE", 16, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.52f));
        SetAnchors(zombieLibraryLabel.rectTransform, 0.407f, 0.035f, 0.493f, 0.115f);
        StyleLibraryLabel(zombieLibraryLabel);

        // Nút CÂY — đặt bên phải cuốn sách Almanac
        var stonePatch = CreateImage("Tấm phủ đá Cây", menuFrame, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        stonePatch.type = Image.Type.Sliced;
        stonePatch.color = new Color(0.22f, 0.32f, 0.16f, 0.97f);
        SetAnchors(stonePatch.rectTransform, 0.500f, 0.032f, 0.585f, 0.118f);
        stonePatch.raycastTarget = false;

        CreateHotspot(menuFrame, "Danh sách cây", 0.495f, 0.025f, 0.590f, 0.145f, new Vector4(0.505f, 0.040f, 0.582f, 0.112f), ShowPlantLibrary);
        var libraryLabel = CreateText("Nhãn danh sách cây", menuFrame, "CÂY", 18, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.65f));
        SetAnchors(libraryLabel.rectTransform, 0.502f, 0.035f, 0.583f, 0.115f);
        StyleLibraryLabel(libraryLabel);
        CreateHotspot(menuFrame, "Trợ giúp", 0.775f, 0.069f, 0.871f, 0.218f, new Vector4(0.790f, 0.085f, 0.855f, 0.165f), ShowHelp);
        CreateHotspot(menuFrame, "Thoát", 0.864f, 0.084f, 0.965f, 0.229f, new Vector4(0.880f, 0.110f, 0.950f, 0.190f), QuitGame);

        noticeText = CreateText("Thông báo", menuFrame, string.Empty, 27, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(noticeText.rectTransform, 0.22f, 0.02f, 0.78f, 0.105f);
        noticeText.gameObject.SetActive(false);

        levelPanel = BuildLevelSelection(menuFrame);
        plantLibraryPanel = BuildPlantLibrary(menuFrame);
        zombieLibraryPanel = BuildZombieLibrary(menuFrame);
        helpPanel = BuildModal(menuFrame, "TRỢ GIÚP",
            "Chọn PHIÊU LƯU để chơi một mình: chọn thẻ cây rồi nhấn vào ô đất để trồng cây chống zombie.\n\n"
            + "CHƠI MẠNG: vào PHIÊU LƯU rồi bấm nút CHƠI MẠNG ở góc trái trên bảng chọn màn. Một người bấm TẠO PHÒNG rồi đọc địa chỉ hiện trên màn hình, người kia bấm THAM GIA và gõ địa chỉ đó vào.\n\n"
            + "Chế độ ĐỒNG ĐỘI: hai người cùng trồng cây, dùng chung kho nắng và dãy thẻ.\n"
            + "Chế độ ĐỐI KHÁNG: chủ phòng giữ phe Cây, người tham gia chỉ huy phe Zombie, tích não để thả quân theo từng hàng.\n\n"
            + "Hai máy phải cùng mạng nội bộ. Nếu chơi qua Internet thì cần mở cổng 7777 hoặc dùng phần mềm tạo mạng ảo.");

        fadeImage = CreateImage("Chuyển cảnh", canvasObject.transform, null);
        Stretch(fadeImage.rectTransform);
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
        fadeImage.canvasRenderer.SetAlpha(0f);
        fadeImage.gameObject.SetActive(false);
    }

    private static readonly Dictionary<string, string> PlantDescriptions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "SunFlower", "Hướng Dương là cây cốt lõi của mọi chiến thuật. Hướng Dương cung cấp nguồn Nắng dồi dào để bạn trồng thêm nhiều cây phòng thủ.\n\n\"Tôi cực kỳ thích ánh nắng mặt trời và những điệu nhảy sôi động dưới ánh ban mai!\"" },
        { "PeaShooter", "Bắn Đậu là phòng tuyến đầu tiên của bạn. Nó bắn ra các viên đậu tròn sát thương cơ bản vào bất kỳ zombie nào bước vào làn đường.\n\n\"Tôi làm việc chăm chỉ, bắn thẳng và luôn nhắm chính xác vào đầu zombie!\"" },
        { "WallNut", "Quả Óc Chó có vỏ giáp siêu dày dặn, dùng làm hàng rào kiên cố chặn đứng bước tiến của binh đoàn zombie.\n\n\"Mọi người hay hỏi tôi có đau khi bị zombie cắn không? Tôi đáp: Không, vỏ tôi dày lắm!\"" },
        { "Squash", "Bí Ép sẽ kiên nhẫn chờ đợi con zombie đầu tiên tiến lại gần, sau đó nhảy cẫng lên và đè bẹp dí đối phương!\n\n\"Tôi sẵn sàng đè bẹp bất kỳ kẻ nào dám xâm phạm khu vườn này!\"" },
        { "TorchWood", "Gốc Đuốc biến các viên đậu bay qua nó thành đậu lửa, tăng gấp đôi sát thương gây ra cho zombie.\n\n\"Đậu qua người tôi là thành đậu lửa hừng hực ngay!\"" },
        { "MiaoMiao", "Mèo Miu là mèo chiến binh dũng cảm, tấn công zombie liên tục với tốc độ cào xé chóng mặt.\n\n\"Meow! Đừng coi thường móng nốt sắc bén của tôi!\"" },
        { "SnowKing", "Vua Băng giá là huyền thoại của vùng tuyết. Triệu hồi trận bão tuyết đóng băng toàn bộ zombie trên màn chơi.\n\n\"Cơn giận của cái lạnh giá sẽ đóng băng mọi kẻ thù!\"" },
        { "SunNut", "Hạt Óc Chó Nắng là sự kết hợp hoàn hảo giữa hàng rào phòng thủ của Quả Óc Chó và khả năng tạo Nắng của Hướng Dương.\n\n\"Tạo nắng và chịu đòn - hai trong một!\"" },
        { "RepeaterPea", "Bắn Đậu Cú Đúp bắn hai viên đậu liên tiếp cùng một lúc, gấp đôi hỏa lực so với Bắn Đậu thường.\n\n\"Hai viên đậu luôn tốt hơn một viên!\"" },
        { "SnowPea", "Bắn Đậu Băng bắn ra các viên đậu băng giá làm chậm tốc độ di chuyển và tốc độ cắn của zombie.\n\n\"Tôi giữ cho bầu không khí luôn tươi mát và lạnh giá.\"" },
        { "Threepeater", "Bắn Đậu 3 Hàng bắn đậu đồng thời trên 3 làn đường xung quanh, phủ rộng hỏa lực toàn sân vườn.\n\n\"Ba cái đầu luôn thông minh và lợi hại hơn một cái head!\"" },
        { "CherryBomb", "Bơm Anh Đào nổ tung ngay lập tức sau khi trồng, thiêu rụi toàn bộ zombie trong phạm vi 3x3.\n\n\"Chúng tôi sẵn sàng nổ tung vì sự bình yên của khu vườn!\"" },
        { "PotatoMine", "Mìn Khoai Tây cần thời gian để chôn mình dưới đất. Sau khi sẵn sàng, nó sẽ phát nổ dẹp gọn zombie dẫm lên.\n\n\"SPUDOW! Kiên nhẫn là chìa khóa của chiến thắng.\"" },
        { "Chomper", "Cây Nuốt Chửng có thể nuốt chửng nguyên một con zombie trong một miếng, nhưng cần thời gian để nhai.\n\n\"Ngon miệng lắm, nhưng tôi cần thời gian để tiêu hóa hết đấy!\"" },
        { "PuffShroom", "Nấm Bắn Gần là loại cây miễn phí (0 Nắng), bắn các bào tử sát thương tầm ngắn.\n\n\"Miễn phí nhưng đầy uy lực ở khoảng cách gần!\"" },
        { "SunShroom", "Nấm Mặt Trời ban đầu cho ít Nắng, nhưng sau một thời gian sẽ lớn lên và tạo Nắng dồi dào như Hướng Dương.\n\n\"Tôi nhỏ bé lúc đầu, nhưng hãy chờ tôi lớn nhé!\"" },
        { "ScaredyShroom", "Nấm Nhát Gan bắn bào tử từ khoảng cách xa, nhưng sẽ sợ hãi chui tọt xuống đất khi zombie lại gần.\n\n\"Tớ ưa khoảng cách an toàn, đừng để chúng tiến lại gần tớ!\"" },
        { "HypnoShroom", "Nấm Thôi Miên khi bị zombie cắn sẽ thôi miên con zombie đó quay lại tấn công các zombie khác.\n\n\"Nhìn sâu vào mắt tôi này... bạn là đồng minh của Cây rồi đấy!\"" },
        { "IceShroom", "Nấm Đóng Băng làm đông cứng tất cả zombie trên toàn bộ màn chơi trong khoảng thời gian ngắn.\n\n\"Đứng yên! Tất cả đông cứng lại cho tôi!\"" },
        { "Jalapeno", "Ớt Cay tạo ra ngọn lửa rực cháy trên toàn bộ một hàng ngang, thiêu rụi mọi zombie ngáng đường.\n\n\"Nóng rực lửa! Không con zombie nào sống sót trên hàng này!\"" },
        { "Spikeweed", "Gai Đất đâm thủng bánh xe và gây sát thương liên tục cho bất kỳ zombie nào bước qua.\n\n\"Hãy cẩn thận từng bước chân trên bãi cỏ này!\"" }
    };

    // These entries are informational only. Hybrid plants are created by combining
    // their component plants during a match, so they must not appear in the seed
    // selection catalogue.
    private static readonly PlantLoadoutEntry[] HybridLibraryEntries =
    {
        new PlantLoadoutEntry("SunNut", "SunNut", "Óc Chó Mặt Trời", "Sprites/Plants/SunNut/States/SunNut0", 125, 7.5f),
        new PlantLoadoutEntry("FireWallNut", "FireWallNut", "Óc Chó Lửa", "Sprites/Plants/FireWallNut/FireWallNutV2", 225, 7.5f),
        new PlantLoadoutEntry("IceWallNut", "IceWallNut", "Óc Chó Băng", "Sprites/Plants/IceWallNut/IceWallNutStates", 125, 30f),
        new PlantLoadoutEntry("PeaTorch", "PeaTorch", "Đậu Đuốc", "Sprites/Plants/Hybrids/PeaTorch/Preview", 275, 1.55f),
        new PlantLoadoutEntry("TorchSun", "TorchSun", "Đuốc Mặt Trời", "Sprites/Plants/Hybrids/TorchSun/Preview", 225, 18f),
        new PlantLoadoutEntry("SunPea", "SunPea", "Đậu Hướng Dương", "Sprites/Plants/Hybrids/SunPea/Preview", 150, 1.7f),
        new PlantLoadoutEntry("SunflowerQueen", "SunflowerQueen", "Nữ Hoàng Hướng Dương", "Sprites/Plants/Hybrids/SunflowerQueen/Preview", 450, 1.35f),
        new PlantLoadoutEntry("CherryShooter", "CherryShooter", "Cherry-shooter", "Sprites/Plants/CherryFusions/CherryShooter", 250, 1.5f),
        new PlantLoadoutEntry("Cherrepeater", "Cherrepeater", "Cherrepeater", "Sprites/Plants/CherryFusions/Cherrepeater", 350, 1.5f),
        new PlantLoadoutEntry("SplitCherry", "SplitCherry", "Split Cherry", "Sprites/Plants/CherryFusions/SplitCherry", 375, 1.5f),
        new PlantLoadoutEntry("GatlingCherry", "GatlingCherry", "Gatling Cherry", "Sprites/Plants/CherryFusions/GatlingCherry", 500, 1.5f),
        new PlantLoadoutEntry("CherryBomber", "CherryBomber", "Cherry-bomber", "Sprites/Plants/CherryFusions/CherryBomber", 400, 1.5f),
        new PlantLoadoutEntry("GatlingCherryBomber", "GatlingCherryBomber", "Gatling Cherrybomber", "Sprites/Plants/CherryFusions/GatlingCherryBomber", 800, 1.5f)
    };

    private static readonly Dictionary<string, (string ing1Name, string ing2Name)> HybridRecipes = new Dictionary<string, (string, string)>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "SunNut", ("Hướng Dương", "Quả Óc Chó") },
        { "FireWallNut", ("Quả Óc Chó", "Gốc Đuốc") },
        { "IceWallNut", ("Quả Óc Chó", "Nấm Băng") },
        { "PeaTorch", ("Bắn Đậu", "Gốc Đuốc") },
        { "TorchSun", ("Gốc Đuốc", "Hướng Dương") },
        { "SunPea", ("Hướng Dương", "Bắn Đậu") },
        { "SunflowerQueen", ("Đậu Đuốc", "Hướng Dương") },
        { "CherryShooter", ("Bắn Đậu", "Cherry Bomb") },
        { "Cherrepeater", ("Bắn Đậu Cú Đúp", "Cherry Bomb") },
        { "SplitCherry", ("Bắn Đậu 3 Hàng", "Cherry Bomb") },
        { "GatlingCherry", ("Cherrepeater / Split Cherry", "Bắn Đậu") },
        { "CherryBomber", ("Cherry-shooter", "Cherry Bomb") },
        { "GatlingCherryBomber", ("Gatling Cherry", "Cherry-bomber") }
    };

    private static readonly Dictionary<string, string> HybridDescriptions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "SunNut", "Óc Chó Mặt Trời được tạo khi kết hợp Hướng Dương với Quả Óc Chó. Nó vừa làm hàng rào kiên cố chắn đường vừa liên tục sản xuất Nắng cho đồng đội." },
        { "FireWallNut", "Óc Chó Lửa được tạo khi kết hợp Quả Óc Chó với Gốc Đuốc. Nó sở hữu vỏ giáp cực dày và ngọn lửa rực cháy thiêu rụi zombie khi chúng cắn phá!" },
        { "IceWallNut", "Óc Chó Băng được tạo khi kết hợp Quả Óc Chó với Nấm Băng. Mỗi cú cắn khiến zombie bị làm chậm, còn lớp giáp băng sẽ nứt dần theo lượng máu còn lại." },
        { "PeaTorch", "Đậu Đuốc được tạo khi kết hợp Bắn Đậu với Gốc Đuốc. Nó trực tiếp bắn ra các viên đậu lửa với hỏa lực gấp đôi mà không cần Gốc Đuốc ngáng đường." },
        { "TorchSun", "Đuốc Mặt Trời được tạo khi kết hợp Gốc Đuốc với Hướng Dương. Nó vừa tỏa ra Nắng ấm áp vừa thiêu rụi bất kỳ zombie nào dẫm phải!" },
        { "SunPea", "Đậu Hướng Dương được tạo khi kết hợp Hướng Dương với Bắn Đậu. Vừa chiến đấu bảo vệ làn đường vừa định kỳ tạo ra Mặt Trời dồi dào!" },
        { "SunflowerQueen", "Nữ Hoàng Hướng Dương là dạng kết hợp tối thượng của cả 3 nguyên tố: Hướng Dương + Bắn Đậu + Gốc Đuốc. Bắn đậu lửa bão táp và tạo vô số Mặt Trời!" },
        { "CherryShooter", "Cherry-shooter bắn một viên Cherry về phía trước, gây 40 sát thương mỗi 1,5 giây." },
        { "Cherrepeater", "Cherrepeater bắn liên tiếp hai viên Cherry trong mỗi đợt tấn công." },
        { "SplitCherry", "Split Cherry bắn một viên về trước và hai viên về sau; đạn sau đổi hướng khi chạm mép trái." },
        { "GatlingCherry", "Gatling Cherry bắn bốn viên Cherry liên tiếp trong mỗi đợt tấn công." },
        { "CherryBomber", "Cherry-bomber bắn đạn Cherry phát nổ, gây sát thương diện rộng khi trúng zombie." },
        { "GatlingCherryBomber", "Dạng tối thượng bắn bốn viên Cherry nổ liên tiếp, mỗi viên gây sát thương diện rộng." }
    };

    private GameObject BuildPlantLibrary(Transform parent)
    {
        var panel = new GameObject("Danh sách cây Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.04f, 0.04f, 0.96f, 0.96f);

        var bgImage = panel.GetComponent<Image>();
        bgImage.sprite = Resources.Load<Sprite>("Sprites/UI/Menu/dialog");
        if (bgImage.sprite != null)
        {
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.90f, 0.85f, 0.75f, 1f);
        }
        else
        {
            bgImage.color = new Color(0.14f, 0.10f, 0.06f, 0.98f);
        }

        var innerBorder = CreateImage("Viền trong", panel.transform, null);
        SetAnchors(innerBorder.rectTransform, 0.015f, 0.02f, 0.985f, 0.98f);
        innerBorder.color = new Color(0.08f, 0.06f, 0.04f, 0.90f);

        var headerBar = CreateImage("Thanh tiêu đề", panel.transform, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        SetAnchors(headerBar.rectTransform, 0.18f, 0.875f, 0.82f, 0.975f);
        headerBar.type = Image.Type.Sliced;
        headerBar.color = new Color(0.24f, 0.32f, 0.18f, 1f);

        var title = CreateText("Tiêu đề", headerBar.transform, "SÁCH TỪ ĐIỂN CÂY TRỒNG", 28, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.40f));
        Stretch(title.rectTransform);
        title.fontStyle = FontStyle.Bold;
        var titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.1f, 0.08f, 0.02f, 0.95f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var subtitle = CreateText("Mô tả phụ", panel.transform, "Tra cứu đặc tính & công thức kết hợp cây trồng", 16, TextAnchor.MiddleCenter, new Color(0.85f, 0.92f, 0.72f));
        SetAnchors(subtitle.rectTransform, 0.20f, 0.83f, 0.80f, 0.875f);

        var normalTab = CreateStoneButton("Tab Cây Thường", panel.transform, "🌿 CÂY THƯỜNG", 17, null);
        SetAnchors(normalTab.GetComponent<RectTransform>(), 0.10f, 0.785f, 0.42f, 0.845f);
        var hybridTab = CreateStoneButton("Tab Cây Kết Hợp", panel.transform, "⚡ CÂY KẾT HỢP", 17, null);
        SetAnchors(hybridTab.GetComponent<RectTransform>(), 0.44f, 0.785f, 0.76f, 0.845f);

        var close = CreateStoneButton("Đóng thư viện cây", panel.transform, "ĐÓNG", 22, () => CloseModal(panel));
        SetAnchors(close.GetComponent<RectTransform>(), 0.84f, 0.885f, 0.97f, 0.965f);

        // CỘT BÊN TRÁI: GRID DANH SÁCH THẺ CÂY
        var leftPanel = CreateImage("Cột danh sách", panel.transform, null);
        SetAnchors(leftPanel.rectTransform, 0.03f, 0.04f, 0.47f, 0.77f);
        leftPanel.color = new Color(0.12f, 0.16f, 0.09f, 0.85f);
        var leftBorder = leftPanel.gameObject.AddComponent<Outline>();
        leftBorder.effectColor = new Color(0.25f, 0.35f, 0.18f, 0.8f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(leftPanel.transform, false);
        SetAnchors(viewport.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.98f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Danh sách Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(8, 8, 10, 10);
        grid.spacing = new Vector2(8f, 8f);
        grid.cellSize = new Vector2(120f, 138f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;

        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = leftPanel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 30f;

        // CỘT BÊN PHẢI: BẢNG XEM CHI TIẾT CÂY
        var rightPanel = CreateImage("Cột chi tiết", panel.transform, null);
        SetAnchors(rightPanel.rectTransform, 0.49f, 0.04f, 0.97f, 0.77f);
        rightPanel.color = new Color(0.18f, 0.14f, 0.10f, 0.92f);
        var rightBorder = rightPanel.gameObject.AddComponent<Outline>();
        rightBorder.effectColor = new Color(0.42f, 0.32f, 0.18f, 0.8f);

        var iconFrame = CreateImage("Khung ảnh cây", rightPanel.transform, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        SetAnchors(iconFrame.rectTransform, 0.06f, 0.62f, 0.38f, 0.94f);
        iconFrame.type = Image.Type.Sliced;
        iconFrame.color = new Color(0.22f, 0.30f, 0.16f, 1f);

        var previewIcon = CreateImage("Ảnh xem trước", iconFrame.transform, null);
        SetAnchors(previewIcon.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
        previewIcon.preserveAspect = true;
        previewIcon.raycastTarget = false;

        var plantTitleText = CreateText("Tên cây chi tiết", rightPanel.transform, "", 24, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.35f));
        SetAnchors(plantTitleText.rectTransform, 0.41f, 0.80f, 0.96f, 0.95f);
        plantTitleText.fontStyle = FontStyle.Bold;
        var pTitleOutline = plantTitleText.gameObject.AddComponent<Outline>();
        pTitleOutline.effectColor = new Color(0.1f, 0.08f, 0.02f, 0.95f);

        var statCostText = CreateText("Giá nắng chi tiết", rightPanel.transform, "", 18, TextAnchor.MiddleLeft, new Color(1f, 0.95f, 0.55f));
        SetAnchors(statCostText.rectTransform, 0.41f, 0.70f, 0.96f, 0.80f);

        var statCooldownText = CreateText("Hồi chiêu chi tiết", rightPanel.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.85f, 0.95f, 0.75f));
        SetAnchors(statCooldownText.rectTransform, 0.41f, 0.60f, 0.96f, 0.70f);

        // Khung Công Thức Kết Hợp (dành riêng cho Cây Kết Hợp)
        var recipeBox = CreateImage("Khung công thức", rightPanel.transform, null);
        SetAnchors(recipeBox.rectTransform, 0.05f, 0.45f, 0.95f, 0.58f);
        recipeBox.color = new Color(0.26f, 0.18f, 0.10f, 0.92f);
        var recipeOutline = recipeBox.gameObject.AddComponent<Outline>();
        recipeOutline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);

        var recipeText = CreateText("Nội dung công thức", recipeBox.transform, "", 15, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.45f));
        Stretch(recipeText.rectTransform);
        recipeText.fontStyle = FontStyle.Bold;

        var descBox = CreateImage("Khung mô tả", rightPanel.transform, null);
        SetAnchors(descBox.rectTransform, 0.05f, 0.04f, 0.95f, 0.43f);
        descBox.color = new Color(0.10f, 0.08f, 0.05f, 0.80f);
        var descOutline = descBox.gameObject.AddComponent<Outline>();
        descOutline.effectColor = new Color(0.35f, 0.26f, 0.15f, 0.6f);

        var descText = CreateText("Nội dung mô tả", descBox.transform, "", 17, TextAnchor.UpperLeft, new Color(0.96f, 0.94f, 0.88f));
        SetAnchors(descText.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
        descText.verticalOverflow = VerticalWrapMode.Truncate;

        var cardBorders = new System.Collections.Generic.List<Image>();

        System.Action<PlantLoadoutEntry, Image> selectPlantAction = (entry, borderImg) =>
        {
            PlayClick();
            foreach (var b in cardBorders)
                if (b != null) b.color = new Color(0.28f, 0.38f, 0.18f, 0.9f);

            if (borderImg != null)
                borderImg.color = new Color(0.95f, 0.90f, 0.30f, 1f);

            previewIcon.sprite = SeedPacketFactory.LoadIcon(entry);
            plantTitleText.text = entry.DisplayName.ToUpper();
            statCostText.text = "☀️  Giá Nắng:  " + entry.Cost;
            statCooldownText.text = "⏱️  Hồi chiêu:  " + entry.Cooldown + " giây";

            if (HybridRecipes.TryGetValue(entry.Key, out var recipe))
            {
                recipeBox.gameObject.SetActive(true);
                recipeText.text = "⚡ CÔNG THỨC KẾT HỢP:\n" + recipe.ing1Name + "   +   " + recipe.ing2Name + "   ➔   " + entry.DisplayName;
                SetAnchors(descBox.rectTransform, 0.05f, 0.04f, 0.95f, 0.43f);
            }
            else
            {
                recipeBox.gameObject.SetActive(false);
                SetAnchors(descBox.rectTransform, 0.05f, 0.04f, 0.95f, 0.57f);
            }

            if (HybridDescriptions.TryGetValue(entry.Key, out var hybridDesc))
                descText.text = hybridDesc;
            else if (PlantDescriptions.TryGetValue(entry.Key, out var desc))
                descText.text = desc;
            else
                descText.text = entry.DisplayName + " là một loài cây phòng thủ tuyệt vời trong khu vườn của bạn!";
        };

        System.Action<bool> showTab = showHybrids =>
        {
            for (int i = content.transform.childCount - 1; i >= 0; i--)
                Destroy(content.transform.GetChild(i).gameObject);
            cardBorders.Clear();

            IEnumerable<PlantLoadoutEntry> entries = showHybrids
                ? HybridLibraryEntries
                : System.Linq.Enumerable.Where(PlantLoadoutCatalog.All, entry => !string.Equals(entry.Key, "SunNut", System.StringComparison.OrdinalIgnoreCase));
            bool firstSelected = false;
            foreach (var entry in entries)
            {
                var card = new GameObject(entry.Key, typeof(RectTransform), typeof(Image), typeof(Button));
                card.transform.SetParent(content.transform, false);
                var cardBg = card.GetComponent<Image>();
                cardBg.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
                cardBg.type = Image.Type.Sliced;
                cardBg.color = new Color(0.28f, 0.38f, 0.18f, 0.9f);
                cardBorders.Add(cardBg);

                var icon = CreateImage("Ảnh cây", card.transform, SeedPacketFactory.LoadIcon(entry));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(icon.rectTransform, 0.08f, 0.34f, 0.92f, 0.94f);

                var nameLabel = CreateText("Tên", card.transform, entry.DisplayName, 15, TextAnchor.MiddleCenter, Color.white);
                SetAnchors(nameLabel.rectTransform, 0.04f, 0.15f, 0.96f, 0.34f);
                nameLabel.raycastTarget = false;

                var costLabel = CreateText("Giá", card.transform, entry.Cost + " ☀️", 15, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f));
                SetAnchors(costLabel.rectTransform, 0.04f, 0.02f, 0.96f, 0.16f);
                costLabel.raycastTarget = false;

                var btn = card.GetComponent<Button>();
                var targetEntry = entry;
                btn.onClick.AddListener(() => selectPlantAction(targetEntry, cardBg));

                if (!firstSelected)
                {
                    firstSelected = true;
                    selectPlantAction(targetEntry, cardBg);
                }
            }

            normalTab.GetComponent<Image>().color = showHybrids ? new Color(0.28f, 0.32f, 0.29f, 0.8f) : new Color(0.35f, 0.55f, 0.20f, 1f);
            hybridTab.GetComponent<Image>().color = showHybrids ? new Color(0.85f, 0.65f, 0.20f, 1f) : new Color(0.28f, 0.32f, 0.29f, 0.8f);
        };

        normalTab.GetComponent<Button>().onClick.AddListener(() => { PlayClick(); showTab(false); });
        hybridTab.GetComponent<Button>().onClick.AddListener(() => { PlayClick(); showTab(true); });
        showTab(false);

        panel.SetActive(false);
        return panel;
    }

    private static readonly Dictionary<string, string> ZombieDescriptions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "ZombieNormal", "Zombie thường là lực lượng cơ bản của đội quân xác sống. Không có giáp bảo vệ, nhưng rẻ và hồi chiêu nhanh.\n\n\"Não... não ngon lắm... cho tôi thêm não đi!\"" },
        { "ConeZombie", "Zombie Mũ Chóp đội một chiếc cọc tiêu để chịu thêm sát thương trước khi bị hạ.\n\n\"Cái mũ này tôi nhặt ở lề đường, nhưng nó hữu ích hơn tôi tưởng!\"" },
        { "ChineseZombie", "Thầy Phù Thủy là một zombie đặc biệt với khả năng gây áp lực lên hàng phòng thủ từ phía sau đội hình.\n\n\"Phép thuật cổ đại không chỉ dùng để diệt quỷ...\"" },
        { "BucketZombie", "Zombie Đội Xô có lớp giáp kim loại rất bền, chịu được lượng sát thương lớn trước khi mất chiếc xô.\n\n\"Ai bảo xô chỉ để đựng nước? Nhìn tôi đây!\"" },
        { "Ghost", "Bóng Ma là kẻ địch bí ẩn, xuất hiện với hình dáng trong suốt và rất khó lường.\n\n\"Bạn không thể bắn thứ bạn không thấy... hehe!\"" },
        { "SnowZombie", "Zombie Tuyết mang sức mạnh giá lạnh, có thể làm đóng băng cây và cản trở hàng phòng thủ.\n\n\"Trời lạnh thì tôi mạnh hơn, tuyết rơi là tôi vui!\"" },
        { "BoneZombie", "Zombie Xương là chiến binh cứng cáp của đội quân xác sống, có sức chống chịu cao.\n\n\"Xương tôi cứng lắm, đậu bắn vào cũng bật ra!\"" },
        { "IceBlockZombie", "Zombie Khối Băng được bảo vệ bởi một khối băng dày, khiến nó trở thành mục tiêu rất khó tiêu diệt.\n\n\"Lớp giáp băng này bất khả xâm phạm... gần như vậy.\"" },
        { "YetiZombie", "Người Tuyết là zombie hiếm và nguy hiểm, sở hữu sức mạnh cùng lượng máu vượt trội.\n\n\"GRAAAH! Tôi là bão tuyết biết đi!\"" },
        { "FlagZombie", "Zombie Cầm Cờ dẫn đầu các đợt tấn công lớn của binh đoàn xác sống.\n\n\"Tôi cầm cờ xông lên, anh em tiến lên nào!\"" },
        { "NewspaperZombie", "Zombie Đọc Báo rất điềm tĩnh cho đến khi tờ báo của hắn bị xé rách!\n\n\"Đang đọc báo hay mà... ĐỪNG LÀM PHIỀN TÔI!\"" },
        { "PoleVaultingZombie", "Zombie Nhảy Sào dùng gậy nhảy vọt qua cây đầu tiên chắn đường.\n\n\"Nhảy cao là sở trường của tôi!\"" },
        { "FootballZombie", "Zombie Cầu Thủ di chuyển cực nhanh và có giáp mũ bảo hiểm rất trâu bò.\n\n\"TOUCHDOWN! Không ai cản nổi tôi!\"" },
        { "ScreenDoorZombie", "Zombie Cầm Cửa dùng cánh cửa lưới sắt chắn toàn bộ đạn đậu bắn thẳng.\n\n\"Cửa sắt kiên cố, đạn đậu búng vào chỉ kêu leng keng!\"" },
        { "BalloonZombie", "Zombie Bóng Bay bay trên không trung, vượt qua hầu hết cây trồng mặt đất.\n\n\"Tôi bay trên cao, cây dưới đất không làm gì được tôi!\"" },
        { "JackinTheBoxZombie", "Zombie Hộp Hề ôm hộp nhạc phát nổ gây sát thương lớn diện rộng.\n\n\"Surprise! Một món quà bất ngờ dành cho khu vườn!\"" },
        { "DancingZombie", "Zombie Vũ Công triệu hồi các vũ công phụ họa vây quanh khu vườn.\n\n\"Let's Dance! Đêm nay là của chúng ta!\"" },
        { "BackupDancer", "Vũ Công Phụ Họa xuất hiện cùng Dancing Zombie để biểu diễn màn nhảy bão táp.\n\n\"Nhảy cùng thần tượng là vinh dự của chúng tôi!\"" },
        { "DolphinRiderZombie", "Zombie Cưỡi Cá Heo lao nhanh trên mặt nước và nhảy qua cây đầu tiên.\n\n\"Cá heo ơi, lao về phía trước nào!\"" },
        { "SnorkelZombie", "Zombie Bơi Lặn chìm dưới nước để né tránh mọi đạn bắn thẳng.\n\n\"Lặn sâu dưới nước, xuất hiện bất ngờ!\"" },
        { "Zomboni", "Xe Dọn Băng đè bẹp mọi cây trồng và cày nát bãi cỏ thành dải băng giá.\n\n\"Brum brum! Xe dọn băng đến đây!\"" },
        { "Imp", "Quỷ Lùn Imp bé nhỏ nhưng di chuyển nhanh và cực kỳ tinh ranh.\n\n\"Tớ nhỏ bé nhưng tớ nhanh nhẹn lắm đấy!\"" }
    };

    // Thông số zombie: HP, tốc độ, loại giáp — dùng hiển thị trong sách từ điển.
    private static readonly Dictionary<string, (int hp, string speed, string armor)> ZombieStats = new Dictionary<string, (int, string, string)>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "ZombieNormal",       (200,  "Chậm",        "Không giáp") },
        { "ConeZombie",         (560,  "Chậm",        "Mũ Chóp") },
        { "ChineseZombie",      (400,  "Trung bình",   "Phép thuật") },
        { "BucketZombie",       (1300, "Chậm",        "Xô sắt") },
        { "Ghost",              (300,  "Nhanh",       "Vô hình") },
        { "SnowZombie",         (600,  "Chậm",        "Băng giá") },
        { "BoneZombie",         (800,  "Chậm",        "Xương cứng") },
        { "IceBlockZombie",     (1500, "Rất chậm",    "Khối băng") },
        { "YetiZombie",         (2000, "Rất chậm",    "Lông dày") },
        { "FlagZombie",         (270,  "Trung bình",   "Cầm cờ") },
        { "NewspaperZombie",    (420,  "Nhanh (cuồng)","Tờ báo") },
        { "PoleVaultingZombie", (500,  "Rất nhanh",   "Gậy nhảy") },
        { "FootballZombie",     (1400, "Cực nhanh",   "Mũ bóng bầu dục") },
        { "ScreenDoorZombie",   (1100, "Chậm",        "Cửa lưới sắt") },
        { "BalloonZombie",      (450,  "Bay lơ lửng",  "Bóng bay") },
        { "JackinTheBoxZombie", (500,  "Nhanh",       "Hộp nổ") },
        { "DancingZombie",      (500,  "Trung bình",   "Vũ công") },
        { "BackupDancer",       (300,  "Trung bình",   "Phụ họa") },
        { "DolphinRiderZombie", (500,  "Rất nhanh",   "Cá heo") },
        { "SnorkelZombie",      (500,  "Trung bình",   "Dưới nước") },
        { "Zomboni",            (1350, "Trung bình",   "Xe bọc thép") },
        { "Imp",                (270,  "Cực nhanh",   "Tí hon") }
    };

    private GameObject BuildZombieLibrary(Transform parent)
    {
        var panel = new GameObject("Danh sách zombie Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.04f, 0.04f, 0.96f, 0.96f);

        var background = panel.GetComponent<Image>();
        background.sprite = Resources.Load<Sprite>("Sprites/UI/Menu/dialog");
        background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = background.sprite != null ? new Color(0.68f, 0.62f, 0.58f, 1f) : new Color(0.10f, 0.06f, 0.06f, 0.98f);

        // Viền trong — tông tím-xám zombie
        var inner = CreateImage("Viền trong", panel.transform, null);
        SetAnchors(inner.rectTransform, 0.015f, 0.02f, 0.985f, 0.98f);
        inner.color = new Color(0.08f, 0.04f, 0.06f, 0.94f);

        // Thanh tiêu đề — tím đậm zombie
        var header = CreateImage("Thanh tiêu đề", panel.transform, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        SetAnchors(header.rectTransform, 0.15f, 0.885f, 0.85f, 0.975f);
        header.type = Image.Type.Sliced;
        header.color = new Color(0.32f, 0.12f, 0.18f, 1f);

        var title = CreateText("Tiêu đề", header.transform, "☠  SÁCH TỪ ĐIỂN ZOMBIE  ☠", 28, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.32f));
        Stretch(title.rectTransform);
        title.fontStyle = FontStyle.Bold;
        StyleLibraryLabel(title);

        var subtitle = CreateText("Mô tả phụ", panel.transform, "Khám phá thông tin chi tiết về đội quân xác sống", 16, TextAnchor.MiddleCenter, new Color(0.85f, 0.72f, 0.68f));
        SetAnchors(subtitle.rectTransform, 0.15f, 0.84f, 0.85f, 0.885f);

        var close = CreateStoneButton("Đóng thư viện zombie", panel.transform, "✕ ĐÓNG", 20, () => CloseModal(panel));
        SetAnchors(close.GetComponent<RectTransform>(), 0.84f, 0.895f, 0.97f, 0.965f);

        // ═══════════════════════════════════════════════
        // CỘT TRÁI: DANH SÁCH ZOMBIE DẠNG GRID
        // ═══════════════════════════════════════════════

        var leftPanel = CreateImage("Cột danh sách", panel.transform, null);
        SetAnchors(leftPanel.rectTransform, 0.025f, 0.035f, 0.46f, 0.825f);
        leftPanel.color = new Color(0.12f, 0.06f, 0.07f, 0.92f);
        var leftBorder = leftPanel.gameObject.AddComponent<Outline>();
        leftBorder.effectColor = new Color(0.45f, 0.20f, 0.18f, 0.7f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(leftPanel.transform, false);
        SetAnchors(viewport.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.98f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var content = new GameObject("Danh sách Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.spacing = new Vector2(7f, 7f);
        grid.cellSize = new Vector2(120f, 160f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = leftPanel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 30f;

        // ═══════════════════════════════════════════════
        // CỘT PHẢI: BẢNG CHI TIẾT ZOMBIE
        // ═══════════════════════════════════════════════

        var detail = CreateImage("Cột chi tiết", panel.transform, null);
        SetAnchors(detail.rectTransform, 0.48f, 0.035f, 0.975f, 0.825f);
        detail.color = new Color(0.12f, 0.07f, 0.065f, 0.95f);
        var detailBorder = detail.gameObject.AddComponent<Outline>();
        detailBorder.effectColor = new Color(0.50f, 0.22f, 0.18f, 0.7f);

        // Khung ảnh zombie lớn
        var iconFrame = CreateImage("Khung ảnh zombie", detail.transform, stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal"));
        SetAnchors(iconFrame.rectTransform, 0.05f, 0.60f, 0.38f, 0.96f);
        iconFrame.type = Image.Type.Sliced;
        iconFrame.color = new Color(0.35f, 0.14f, 0.14f, 1f);
        var iconInnerBorder = iconFrame.gameObject.AddComponent<Outline>();
        iconInnerBorder.effectColor = new Color(0.65f, 0.30f, 0.22f, 0.8f);

        var preview = CreateImage("Ảnh xem trước", iconFrame.transform, null);
        SetAnchors(preview.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
        preview.preserveAspect = true;
        preview.raycastTarget = false;

        // Tên zombie
        var nameText = CreateText("Tên zombie", detail.transform, "", 26, TextAnchor.MiddleLeft, new Color(1f, 0.80f, 0.30f));
        SetAnchors(nameText.rectTransform, 0.41f, 0.85f, 0.96f, 0.96f);
        nameText.fontStyle = FontStyle.Bold;
        var nameOutline = nameText.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0.12f, 0.06f, 0.04f, 0.95f);
        nameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Thanh phân cách nhỏ dưới tên
        var nameSeparator = CreateImage("Phân cách tên", detail.transform, null);
        SetAnchors(nameSeparator.rectTransform, 0.41f, 0.84f, 0.94f, 0.845f);
        nameSeparator.color = new Color(0.65f, 0.30f, 0.22f, 0.7f);

        // Khung thông số — nền tối hơn
        var statsBox = CreateImage("Khung thông số", detail.transform, null);
        SetAnchors(statsBox.rectTransform, 0.41f, 0.60f, 0.96f, 0.83f);
        statsBox.color = new Color(0.08f, 0.04f, 0.04f, 0.80f);
        var statsOutline = statsBox.gameObject.AddComponent<Outline>();
        statsOutline.effectColor = new Color(0.40f, 0.18f, 0.14f, 0.6f);

        // Chi phí não
        var costText = CreateText("Giá não", statsBox.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.98f, 0.60f, 0.68f));
        SetAnchors(costText.rectTransform, 0.06f, 0.72f, 0.96f, 0.96f);

        // HP
        var hpText = CreateText("Máu", statsBox.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.95f, 0.40f, 0.35f));
        SetAnchors(hpText.rectTransform, 0.06f, 0.48f, 0.96f, 0.72f);

        // Tốc độ
        var speedText = CreateText("Tốc độ", statsBox.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.75f, 0.88f, 0.95f));
        SetAnchors(speedText.rectTransform, 0.06f, 0.24f, 0.96f, 0.48f);

        // Giáp
        var armorText = CreateText("Giáp", statsBox.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.90f, 0.82f, 0.65f));
        SetAnchors(armorText.rectTransform, 0.06f, 0.00f, 0.96f, 0.24f);

        // Hồi chiêu
        var cooldownText = CreateText("Hồi chiêu", detail.transform, "", 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.80f, 0.72f));
        SetAnchors(cooldownText.rectTransform, 0.06f, 0.53f, 0.96f, 0.59f);

        // Thanh phân cách trước mô tả
        var descSeparator = CreateImage("Phân cách mô tả", detail.transform, null);
        SetAnchors(descSeparator.rectTransform, 0.06f, 0.515f, 0.94f, 0.52f);
        descSeparator.color = new Color(0.50f, 0.24f, 0.18f, 0.6f);

        // Khung mô tả
        var descBox = CreateImage("Khung mô tả", detail.transform, null);
        SetAnchors(descBox.rectTransform, 0.04f, 0.04f, 0.96f, 0.51f);
        descBox.color = new Color(0.08f, 0.05f, 0.04f, 0.82f);
        var descBoxOutline = descBox.gameObject.AddComponent<Outline>();
        descBoxOutline.effectColor = new Color(0.32f, 0.16f, 0.12f, 0.5f);

        var description = CreateText("Mô tả", descBox.transform, "", 17, TextAnchor.UpperLeft, new Color(0.94f, 0.90f, 0.84f));
        SetAnchors(description.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
        description.verticalOverflow = VerticalWrapMode.Truncate;

        // ═══════════════════════════════════════════════
        // XỬ LÝ CHỌN ZOMBIE + TẠO THẺ
        // ═══════════════════════════════════════════════

        var borders = new List<Image>();
        System.Action<ZombieRoster.Entry, Image> selectZombie = (entry, border) =>
        {
            PlayClick();
            foreach (var item in borders) item.color = new Color(0.32f, 0.14f, 0.12f, 0.94f);
            border.color = new Color(0.90f, 0.48f, 0.20f, 1f);

            preview.sprite = LoadZombieIcon(entry.name);
            nameText.text = entry.label.ToUpper();
            costText.text = "🧠  Giá Não:  " + entry.cost;
            cooldownText.text = "⏱  Hồi chiêu:  " + entry.cooldown + " giây";

            if (ZombieStats.TryGetValue(entry.name, out var stats))
            {
                hpText.text = "❤️  Máu:  " + stats.hp;
                speedText.text = "💨  Tốc độ:  " + stats.speed;
                armorText.text = "🛡️  Giáp:  " + stats.armor;
            }
            else
            {
                hpText.text = "❤️  Máu:  ???";
                speedText.text = "💨  Tốc độ:  ???";
                armorText.text = "🛡️  Giáp:  ???";
            }

            description.text = ZombieDescriptions.TryGetValue(entry.name, out var value) ? value : "Một thành viên nguy hiểm của đội quân zombie.";
        };

        bool selected = false;
        foreach (var entry in ZombieRoster.All)
        {
            var card = new GameObject(entry.name, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(content.transform, false);
            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.32f, 0.14f, 0.12f, 0.94f);
            borders.Add(cardImage);

            // Ảnh zombie trên thẻ
            var icon = CreateImage("Ảnh zombie", card.transform, LoadZombieIcon(entry.name));
            SetAnchors(icon.rectTransform, 0.08f, 0.38f, 0.92f, 0.94f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Tên zombie
            var label = CreateText("Tên", card.transform, entry.label, 14, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(label.rectTransform, 0.04f, 0.20f, 0.96f, 0.38f);
            label.raycastTarget = false;

            // Dòng giá não
            var price = CreateText("Giá", card.transform, entry.cost + " 🧠", 13, TextAnchor.MiddleCenter, new Color(1f, 0.68f, 0.68f));
            SetAnchors(price.rectTransform, 0.04f, 0.10f, 0.96f, 0.22f);
            price.raycastTarget = false;

            // Dòng HP nhỏ trên thẻ
            string hpLabel = "???";
            if (ZombieStats.TryGetValue(entry.name, out var st))
                hpLabel = "❤️ " + st.hp;
            var hpSmall = CreateText("HP nhỏ", card.transform, hpLabel, 12, TextAnchor.MiddleCenter, new Color(0.95f, 0.42f, 0.38f));
            SetAnchors(hpSmall.rectTransform, 0.04f, 0.01f, 0.96f, 0.11f);
            hpSmall.raycastTarget = false;

            var capturedEntry = entry;
            card.GetComponent<Button>().onClick.AddListener(() => selectZombie(capturedEntry, cardImage));
            if (!selected)
            {
                selected = true;
                selectZombie(capturedEntry, cardImage);
            }
        }

        panel.SetActive(false);
        return panel;
    }

    private static Sprite LoadZombieIcon(string zombieName)
    {
        return ZombieIconHelper.GetIcon(zombieName);
    }

    private static void StyleLibraryLabel(Text label)
    {
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void CreateHotspot(Transform parent, string label, float xMin, float yMin, float xMax, float yMax, Vector4 textRect, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Nút " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);

        var motion = go.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = image;
        motion.highlightMenuText = true;
        motion.menuController = this;
        motion.hoverRect = textRect;
    }

    // Mở sảnh chờ chơi mạng, mang theo màn đang chọn để chủ phòng khỏi phải chọn lại.
    private void OpenNetLobby()
    {
        if (transitioning) return;
        PlayClick();
        NetLobbyUI.Open(selectedLevel);
    }

    private GameObject BuildModal(Transform parent, string title, string body)
    {
        var panel = new GameObject(title + " Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.22f, 0.24f, 0.78f, 0.76f);
        panel.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.06f, 0.95f);

        var titleText = CreateText("Tiêu đề", panel.transform, title, 44, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.24f));
        SetAnchors(titleText.rectTransform, 0.08f, 0.72f, 0.92f, 0.94f);

        var bodyText = CreateText("Nội dung", panel.transform, body, 25, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(bodyText.rectTransform, 0.09f, 0.28f, 0.91f, 0.72f);

        var close = new GameObject("Đóng", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        close.transform.SetParent(panel.transform, false);
        SetAnchors(close.GetComponent<RectTransform>(), 0.34f, 0.07f, 0.66f, 0.24f);
        var closeImage = close.GetComponent<Image>();
        closeImage.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        closeImage.type = Image.Type.Sliced;
        var closeButton = close.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(() => CloseModal(panel));
        close.GetComponent<MenuButtonMotion>().targetGraphic = closeImage;
        var closeText = CreateText("Chữ", close.transform, "ĐÓNG", 27, TextAnchor.MiddleCenter, Color.white);
        Stretch(closeText.rectTransform);
        closeText.raycastTarget = false;

        panel.SetActive(false);
        return panel;
    }

    private GameObject BuildLevelSelection(Transform parent)
    {
        var panel = new GameObject("Chọn màn chơi Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.055f, 0.065f, 0.945f, 0.935f);
        panel.GetComponent<Image>().color = new Color(0.075f, 0.10f, 0.055f, 0.975f);

        var title = CreateText("Tiêu đề", panel.transform, "CHỌN MÀN CHƠI", 42, TextAnchor.MiddleCenter, new Color(0.62f, 1f, 0.25f));
        SetAnchors(title.rectTransform, 0.27f, 0.855f, 0.73f, 0.97f);

        // Góc trái trên: lối vào chế độ chơi mạng hai người, dùng chung màn đang chọn bên dưới.
        var netButton = CreateStoneButton("Chơi mạng", panel.transform, "CHƠI MẠNG", 22, OpenNetLobby);
        SetAnchors(netButton.GetComponent<RectTransform>(), 0.032f, 0.878f, 0.235f, 0.968f);

        var netHint = CreateText("Chú thích chơi mạng", panel.transform, "2 người", 17,
            TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.66f));
        SetAnchors(netHint.rectTransform, 0.032f, 0.828f, 0.235f, 0.874f);
        netHint.raycastTarget = false;

        var close = CreateStoneButton("Đóng chọn màn", panel.transform, "X", 26, () => CloseModal(panel));
        SetAnchors(close.GetComponent<RectTransform>(), 0.91f, 0.88f, 0.975f, 0.965f);

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(panel.transform, false);
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        SetAnchors(viewportRect, 0.045f, 0.165f, 0.955f, 0.845f);
        viewportObject.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.12f, 0.28f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;

        var contentObject = new GameObject("Danh sách màn", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        var contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        var grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.spacing = new Vector2(12f, 14f);
        grid.cellSize = new Vector2(142f, 130f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.12f;
        scroll.inertia = true;
        scroll.scrollSensitivity = 34f;

        string[] levelNames =
        {
            "Mèo Miu Xuất Trận",
            "Hành Trình Mới",
            "Thầy Luyện Xác",
            "Vùng Đất Bất Tử",
            "Sông Băng Địa Cực",
            "Sân Thử Nghiệm",
            "Đấu Trường Gargantuar",
            "Chiến Dịch Ba Cõi"
        };
        string[] thumbnails =
        {
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Night_Wall",
            "Sprites/BackGround/background_Night_Bone",
            "Sprites/BackGround/Background_Ice",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/background_Night_Bone"
        };

        for (int i = 0; i < levelNames.Length; i++)
            CreateLevelCard(contentObject.transform, i, levelNames[i], thumbnails[i]);

        selectedLevelText = CreateText("Màn đã chọn", panel.transform, "Hãy chọn một màn chơi", 24, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(selectedLevelText.rectTransform, 0.10f, 0.055f, 0.62f, 0.15f);

        playLevelButton = CreateStoneButton("Chơi", panel.transform, "CHƠI", 29, PlaySelectedLevel).GetComponent<Button>();
        SetAnchors(playLevelButton.GetComponent<RectTransform>(), 0.66f, 0.045f, 0.88f, 0.15f);
        playLevelButton.interactable = false;

        panel.SetActive(false);
        return panel;
    }

    private void CreateLevelCard(Transform parent, int levelIndex, string levelName, string thumbnailPath)
    {
        var card = new GameObject("Màn " + (levelIndex + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        card.transform.SetParent(parent, false);
        var frame = card.GetComponent<Image>();
        frame.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.74f, 0.76f, 0.70f, 1f);
        levelCardFrames.Add(frame);

        var button = card.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        int capturedLevel = levelIndex;
        button.onClick.AddListener(() => SelectLevel(capturedLevel));
        card.GetComponent<MenuButtonMotion>().targetGraphic = frame;

        var thumbnail = CreateImage("Ảnh màn", card.transform, Resources.Load<Sprite>(thumbnailPath));
        SetAnchors(thumbnail.rectTransform, 0.075f, 0.39f, 0.925f, 0.90f);
        thumbnail.raycastTarget = false;

        var label = CreateText("Tên màn", card.transform, "MÀN " + (levelIndex + 1) + "\n" + levelName, 21, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(label.rectTransform, 0.07f, 0.045f, 0.93f, 0.38f);
        label.raycastTarget = false;
    }

    private GameObject CreateStoneButton(string name, Transform parent, string label, int fontSize, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        image.type = Image.Type.Sliced;
        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        // Tabs attach their callback after creation. Do not register a null
        // UnityAction: UnityEvent will try to invoke it when the button is pressed.
        if (action != null)
            button.onClick.AddListener(action);
        go.GetComponent<MenuButtonMotion>().targetGraphic = image;
        var text = CreateText("Chữ", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return go;
    }

    private void SelectLevel(int levelIndex)
    {
        selectedLevel = levelIndex;
        for (int i = 0; i < levelCardFrames.Count; i++)
            levelCardFrames[i].color = i == levelIndex
                ? new Color(0.64f, 1f, 0.30f, 1f)
                : new Color(0.74f, 0.76f, 0.70f, 1f);

        string[] names = { "Mèo Miu Xuất Trận", "Hành Trình Mới", "Thầy Luyện Xác", "Vùng Đất Bất Tử", "Sông Băng Địa Cực", "Sân Thử Nghiệm", "Đấu Trường Gargantuar", "Chiến Dịch Ba Cõi" };
        selectedLevelText.text = "Đã chọn: Màn " + (levelIndex + 1) + " — " + names[levelIndex];
        playLevelButton.interactable = true;
        PlayClick();
    }

    private void PlaySelectedLevel()
    {
        if (selectedLevel < 0 || transitioning) return;

        if (selectedLevel == GargantuarArenaLevelIndex)
        {
            StartCoroutine(LoadSceneWithFade(GargantuarArenaBootstrap.SceneName));
            return;
        }

        if (selectedLevel == ThreeWorldsCampaignLevelIndex)
        {
            StartCoroutine(LoadSceneWithFade(CampaignBootstrap.GameScene));
            return;
        }

        GameSession.SelectedLevel = selectedLevel;
        PlantSelectionOverlay.Show(selectedLevel, () => StartCoroutine(LoadSceneWithFade("GameScene")));
    }

    private void ShowPlantLibrary() => OpenModal(plantLibraryPanel);
    private void ShowZombieLibrary() => OpenModal(zombieLibraryPanel);
    private void ShowHelp() => OpenModal(helpPanel);

    private void OpenModal(GameObject panel)
    {
        if (transitioning || panel == null) return;
        panel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(panel.GetComponent<CanvasGroup>(), 0f, 1f, 0.2f, false));
    }

    private void CloseModal(GameObject panel)
    {
        if (panel != null)
            StartCoroutine(FadeCanvasGroup(panel.GetComponent<CanvasGroup>(), panel.GetComponent<CanvasGroup>().alpha, 0f, 0.16f, true));
    }

    private void ShowNotice(string message)
    {
        if (!transitioning)
            StartCoroutine(ShowNoticeRoutine(message));
    }

    private IEnumerator ShowNoticeRoutine(string message)
    {
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);
        noticeText.canvasRenderer.SetAlpha(0f);
        noticeText.CrossFadeAlpha(1f, 0.15f, true);
        yield return new WaitForSecondsRealtime(1.8f);
        noticeText.CrossFadeAlpha(0f, 0.25f, true);
        yield return new WaitForSecondsRealtime(0.25f);
        noticeText.gameObject.SetActive(false);
    }

    private void StartAdventure()
    {
        if (!transitioning)
            OpenModal(levelPanel);
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        transitioning = true;
        PlayClick();
        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(0f);
        fadeImage.CrossFadeAlpha(1f, 0.48f, true);
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator AnimateEntrance()
    {
        float elapsed = 0f;
        while (elapsed < 0.7f)
        {
            elapsed += Time.unscaledDeltaTime;
            menuGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 0.7f);
            yield return null;
        }
        menuGroup.alpha = 1f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, bool disableAfter)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
        if (disableAfter) group.gameObject.SetActive(false);
    }

    private void QuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClick()
    {
        var clip = Resources.Load<AudioClip>("Sounds/UI/graveButtonClick");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    public void SetMenuTextHighlight(Vector4 hoverRect, bool highlighted)
    {
        if (menuHoverMaterial == null) return;
        if (highlighted)
        {
            menuHoverMaterial.SetVector("_HoverRect", hoverRect);
            menuHoverTarget = 1f;
        }
        else
        {
            menuHoverTarget = 0f;
        }
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = menuFont;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 15;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, 0f, 0f, 1f, 1f);
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void EnsureMenuCamera()
    {
        if (Camera.main != null || Object.FindAnyObjectByType<Camera>() != null)
            return;

        var cameraObject = new GameObject("Main Menu Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.depth = -100f;
    }
}

public static class GameSession
{
    public static int SelectedLevel = -1;
    public static readonly List<string> SelectedPlants = new List<string>();
}

public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Graphic targetGraphic;
    public bool highlightMenuText;
    public MainMenuController menuController;
    public Vector4 hoverRect;
    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        if (!highlightMenuText)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 14f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 1.025f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, false);
        else
            targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 0.965f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 1.025f;
    }
}
