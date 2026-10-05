using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Menu tạm dừng dựng ở runtime để dùng thống nhất cho mọi màn chơi.</summary>
public class GameplayPauseMenu : MonoBehaviour
{
    private static GameplayPauseMenu instance;

    private GameObject pauseButton;
    private GameObject informationButton;
    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private GameObject informationOverlay;
    private CanvasGroup informationGroup;
    private Text informationTitle;
    private Text informationBody;
    private Text informationPageText;
    private Button informationNextButton;
    private GameObject map9HudGuide;
    private Image map9HudGuideStatus;
    private Text map9HudGuideTitle;
    private Text map9HudGuideDetail;
    private Image map9HudGuideMeter;
    private AudioSource uiAudio;
    private Font font;
    private Image soundStateImage;
    private bool isPaused;
    private bool isInformationOpen;
    private bool animating;
    private int informationPage;

    private const int Map9LevelIndex = 8;
    private static readonly string[] Map9GuideStatusNames =
        { "waiting", "active", "active", "success", "failure" };
    private static readonly string[] Map9GuideTitles =
    {
        "MẠCH KẾ TIẾP", "KÍCH HOẠT MẠCH: 2/4", "NODE MỤC TIÊU",
        "XUNG ĐIỆN HOÀN TẤT!", "MẠCH BỊ QUÁ TẢI"
    };
    private static readonly string[] Map9GuideDetails =
    {
        "Sẵn sàng sau 10 giây", "Trồng đủ node • còn 24 giây",
        "Xanh: trống • Vàng: đã có cây", "+75 nắng • zombie bị khống chế",
        "Cây trên node mất 80 máu"
    };
    private static readonly float[] Map9GuideProgress = { 0f, .62f, .35f, 1f, 0f };

    // Nội dung được chia trang để luôn nằm gọn trong phần giấy của box.
    private static readonly string[][][] LevelInformationPages =
    {
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nBảo vệ ngôi nhà qua toàn bộ các đợt tấn công. Thu thập nắng, trồng cây trên 5 hàng và không để zombie chạm vạch cuối sân.",
                "GAMEPLAY\n\nĐây là màn giới thiệu đội hình đầy đủ. Zombie Tuyết làm lạnh cây; Yeti có sức chịu đựng cao. Mèo Miu là cây đặc biệt của màn, thích hợp xử lý mục tiêu nguy hiểm.",
                "CÁCH CHƠI\n\nƯu tiên Hướng Dương ở phía sau, dựng phòng tuyến Óc Chó rồi đặt cây bắn phía trong. Giữ Bí Đao cho hàng bị thủng và tập trung hỏa lực khi Yeti xuất hiện."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nHoàn thành màn hướng dẫn cơ bản chỉ với Hướng Dương và Đậu Bắn. Sống sót đến khi zombie cuối cùng bị tiêu diệt.",
                "GAMEPLAY\n\nMàn này tập trung vào nhịp kinh tế và vị trí đặt cây. Hướng Dương tạo nắng; Đậu Bắn tấn công zombie trong cùng hàng.",
                "CÁCH CHƠI\n\nTrồng 2–3 Hướng Dương trước, sau đó đặt ít nhất một Đậu Bắn ở hàng có zombie. Luôn để dành 100 nắng để phản ứng với hàng mới bị tấn công."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nNgăn các đội Cương Thi xếp hàng vượt qua bức tường. Mỗi lần sinh, chúng xuất hiện thành nhóm 3–5 con trên cùng một tuyến.",
                "GAMEPLAY\n\nCương Thi đi theo đội hình nối đuôi. Áp lực của màn nằm ở một hàng tăng đột ngột thay vì zombie rải đều trên cả sân.",
                "CÁCH CHƠI\n\nKhông chia sát thương quá mỏng. Đặt Óc Chó để kéo dài thời gian bắn, tăng hỏa lực cho hàng đang có đoàn đông và giữ một lượng nắng dự phòng."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nDọn sạch Vùng Đất Bất Tử và sống sót qua các đợt Bone Zombie. Đừng vội coi một zombie vừa ngã xuống là đã bị loại.",
                "GAMEPLAY\n\nBone Zombie có 3 mạng và có thể hồi sinh sau khoảng 20–30 giây. Bóng Ma bắt đầu xuất hiện trong trận và tiến vào từ nửa sau bãi cỏ.",
                "CÁCH CHƠI\n\nDuy trì hỏa lực lâu dài trên mọi hàng. Dùng Bí Đao cho tình huống khẩn cấp, nhưng vẫn chuẩn bị cho lần hồi sinh tiếp theo của Bone Zombie."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nGiữ phòng tuyến trên Sông Băng cho tới đợt cuối. Chống lại Zombie Tuyết, Yeti và những đợt zombie có giáp.",
                "GAMEPLAY\n\nSau 40 giây, giá lạnh bắt đầu đóng băng cây ngẫu nhiên. Cây bị lạnh hoạt động chậm và liên tục mất máu; các lần đóng băng về sau xảy ra thường xuyên hơn.",
                "CÁCH CHƠI\n\nDùng Torchwood làm nguồn sưởi và đặt gần các cây chủ lực. Tránh phụ thuộc vào một cây duy nhất; dựng nhiều lớp phòng thủ để chịu được thời điểm cây bị lạnh."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nĐây là khu thử nghiệm tự do dành cho cây, zombie và hệ thống Fusion. Thẻ cây không tốn nắng và không có thời gian hồi.",
                "ĐIỀU KHIỂN THỬ NGHIỆM\n\nTrên máy tính: phím 1–9 chọn zombie có sẵn, F1 chọn Flag Zombie, F2 chọn Newspaper Zombie. Nhấp chuột phải lên một hàng để sinh zombie đã chọn.",
                "THỬ FUSION\n\nTrồng một cây rồi chọn cây tương thích và bấm lại đúng ô để hợp thể. Hãy thử nhiều thứ tự kết hợp, quan sát sát thương, hiệu ứng và khả năng tạo nắng."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nSống sót trong Rừng Nhật Thực khi ánh sáng và bóng tối liên tục thay đổi. Theo dõi HUD để biết pha tiếp theo.",
                "GAMEPLAY\n\nKhi nhật thực bắt đầu, nắng trời tạm ngừng. Zombie trong hai hàng phủ bóng được tăng 20% tốc độ. Sau 10 giây, ánh sáng trở lại trong 20 giây.",
                "CÁCH CHƠI\n\nTạo kinh tế trước lần nhật thực đầu tiên. Dùng Nấm Mặt Trời và Hướng Dương để bớt phụ thuộc nắng trời; gia cố ngay hai hàng đang bị phủ bóng."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU\n\nBảo vệ Đảo Thiên Đường qua các chu kỳ nước ròng, cảnh báo và triều dâng. Sáu cột bên trái luôn là vùng an toàn.",
                "GAMEPLAY\n\nBa cột ven biển bị ngập khi triều lên; mỗi chu kỳ thứ ba ngập thêm một cột. Không thể trồng trên ô ngập, cây tại đó mất máu, còn zombie bị chậm và đẩy lùi.",
                "CÁCH CHƠI\n\nKhi nước ròng, trồng trên hai ô Cát Vàng để nhận 25 nắng. Giữ cây sống đến lúc sóng tới để nhận thêm 50 nắng, nhưng đừng đặt cây chủ lực quá sát biển."
            }
        },
        new[]
        {
            new[]
            {
                "MỤC TIÊU MẠCH NĂNG LƯỢNG\n\nBảo vệ đủ 5 hàng và hoàn thành các mạch xuất hiện liên tục. Mỗi mạch nối 4 node trên sân; trồng một cây hợp lệ lên từng node trước khi đồng hồ về 0.",
                "CÁCH ĐỌC HUD\n\nBiểu tượng bên trái cho biết trạng thái mạch. Dòng lớn hiển thị số node đã có cây (ví dụ 2/4); dòng nhỏ là hướng dẫn hoặc thời gian còn lại. Timeline xanh thu dần từ phải sang trái theo thời gian còn lại.",
                "NODE VÀ ĐƯỜNG NỐI\n\nNode xanh nhấp nháy là ô mục tiêu đang trống. Node chuyển vàng khi cây đã đứng đúng ô; dây năng lượng nối các node theo thứ tự mạch. Nếu cây bị phá, node lập tức trở lại trạng thái chưa hoàn thành.",
                "HOÀN THÀNH MẠCH\n\nGiữ đủ 4 cây trong 0,5 giây để kích hoạt xung điện: mọi zombie nhận 140 sát thương, bị đóng băng 2,5 giây rồi làm chậm thêm 5 giây. Người chơi đồng thời nhận 75 nắng.",
                "QUÁ TẢI VÀ CHIẾN THUẬT\n\nHết 26 giây khi chưa đủ node sẽ tạo hiệu ứng quá tải đỏ và mỗi cây đang đứng trên node mất 80 máu. Mạch mới ưu tiên vùng ít cây, vì vậy hãy dùng Puff-shroom cho node xa, giữ nắng dự phòng và tận dụng 10 giây nghỉ."
            }
        }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        UiAudioSettings.ApplySavedState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null || Object.FindAnyObjectByType<GameManagement>() == null) return;

        var root = new GameObject("Menu tạm dừng", typeof(GameplayPauseMenu), typeof(AudioSource));
        instance = root.GetComponent<GameplayPauseMenu>();
        instance.Build();
    }

    private void Build()
    {
        font = Resources.Load<Font>("Fonts/Baloo2");
        uiAudio = GetComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.ignoreListenerPause = true;

        var canvasObject = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = .5f;

        EnsureEventSystem();

        pauseButton = CreateTextureButton("Nút Pause", canvasObject.transform, LoadUiSprite("pause"), TogglePause);
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-16f, -14f);
        pauseRect.sizeDelta = new Vector2(62f, 68f);
        AddSoftShadow(pauseButton, new Vector2(2f, -3f));

        informationButton = CreateTextureButton("Nút Thông tin", canvasObject.transform,
            LoadUiSprite("icon_information"), OpenInformation);
        RectTransform informationRect = informationButton.GetComponent<RectTransform>();
        informationRect.anchorMin = informationRect.anchorMax = new Vector2(1f, 1f);
        informationRect.pivot = new Vector2(1f, 1f);
        informationRect.anchoredPosition = new Vector2(-84f, -14f);
        informationRect.sizeDelta = new Vector2(62f, 66f);
        AddSoftShadow(informationButton, new Vector2(2f, -3f));

        overlay = new GameObject("Lớp tạm dừng", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvasObject.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, .67f);
        overlayGroup = overlay.GetComponent<CanvasGroup>();

        Image settingIcon = CreateImage("Biểu tượng cài đặt", overlay.transform, LoadUiSprite("setting"));
        Center(settingIcon.rectTransform, new Vector2(49f, 54f), new Vector2(-92f, 242f));
        settingIcon.raycastTarget = false;
        Text title = CreateText("Tiêu đề", overlay.transform, "TẠM DỪNG", 31, new Color(1f, .93f, .48f));
        Center(title.rectTransform, new Vector2(210f, 50f), new Vector2(27f, 241f));

        float y = 154f;
        CreateMenuRow(overlay.transform, "TIẾP TỤC", new Vector2(-20f, y), true, "play", Resume);
        CreateSoundRow(overlay.transform, new Vector2(20f, y - 77f), false);

        if (NetSession.IsOnline)
            CreateMenuRow(overlay.transform, "RỜI TRẬN", new Vector2(-20f, y - 154f), true, "return", ReturnToMainMenu);
        else
            CreateMenuRow(overlay.transform, "CHƠI LẠI", new Vector2(-20f, y - 154f), true, "confirm", RestartLevel);

        CreateMenuRow(overlay.transform, "VỀ MENU", new Vector2(20f, y - 231f), false, "return", ReturnToMainMenu);
        CreateMenuRow(overlay.transform, "THOÁT GAME", new Vector2(-20f, y - 308f), true, "cancel", QuitGame);

        overlay.SetActive(false);

        BuildInformationOverlay(canvasObject.transform);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (isInformationOpen) CloseInformation();
        else TogglePause();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!NetSession.IsOnline && !hasFocus && !isPaused && Time.timeScale > 0f)
            Pause();
    }

    private void TogglePause()
    {
        if (animating) return;
        if (isPaused) Resume();
        else if (Time.timeScale > 0f) Pause();
    }

    private void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        pauseButton.SetActive(false);
        informationButton.SetActive(false);
        overlay.SetActive(true);
        overlayGroup.alpha = 0f;

        if (!NetSession.IsOnline)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        PlayClick();
        StartCoroutine(FadeOverlay(0f, 1f, .18f, false));
    }

    private void Resume()
    {
        if (!isPaused || animating) return;
        PlayClick();
        StartCoroutine(FadeOverlay(overlayGroup.alpha, 0f, .16f, true));
    }

    private IEnumerator FadeOverlay(float from, float to, float duration, bool resumeAfter)
    {
        animating = true;
        float elapsed = 0f;
        overlayGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        overlayGroup.alpha = to;
        animating = false;

        if (!resumeAfter) yield break;
        overlay.SetActive(false);
        pauseButton.SetActive(true);
        informationButton.SetActive(true);
        isPaused = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void ToggleSound()
    {
        bool enabled = UiAudioSettings.Toggle();
        UpdateSoundState(enabled);
        if (enabled) PlayClick();
    }

    private void UpdateSoundState(bool enabled)
    {
        if (soundStateImage != null)
            soundStateImage.sprite = LoadUiSprite(enabled ? "on" : "off");
    }

    private void RestartLevel()
    {
        if (animating) return;
        PlayClick();
        RestoreGameState();
        SceneManager.LoadScene("GameScene");
    }

    private void ReturnToMainMenu()
    {
        if (animating) return;
        PlayClick();
        RestoreGameState();
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        PlayClick();
        RestoreGameState();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestoreGameState()
    {
        isPaused = false;
        isInformationOpen = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void BuildInformationOverlay(Transform canvas)
    {
        informationOverlay = new GameObject("Lớp thông tin màn chơi", typeof(RectTransform),
            typeof(Image), typeof(CanvasGroup));
        informationOverlay.transform.SetParent(canvas, false);
        Stretch(informationOverlay.GetComponent<RectTransform>());
        informationOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, .72f);
        informationGroup = informationOverlay.GetComponent<CanvasGroup>();

        Image noticeBox = CreateImage("Khung hướng dẫn", informationOverlay.transform,
            LoadUiSprite("ui_notice_boxchat"));
        Center(noticeBox.rectTransform, new Vector2(720f, 517f), Vector2.zero);
        noticeBox.raycastTarget = true;

        informationTitle = CreateText("Tên màn", noticeBox.transform, string.Empty, 25,
            new Color(1f, .91f, .55f));
        SetAnchors(informationTitle.rectTransform, .17f, .805f, .83f, .965f);
        informationTitle.resizeTextMinSize = 17;

        informationBody = CreateText("Nội dung hướng dẫn", noticeBox.transform, string.Empty, 21,
            new Color(.22f, .10f, .035f));
        SetAnchors(informationBody.rectTransform, .095f, .145f, .905f, .785f);
        informationBody.alignment = TextAnchor.UpperLeft;
        informationBody.fontStyle = FontStyle.Normal;
        informationBody.resizeTextMinSize = 17;
        informationBody.resizeTextMaxSize = 21;
        informationBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        informationBody.verticalOverflow = VerticalWrapMode.Truncate;
        informationBody.raycastTarget = false;
        Outline bodyOutline = informationBody.GetComponent<Outline>();
        if (bodyOutline != null) bodyOutline.enabled = false;

        BuildMap9HudGuide(noticeBox.transform);

        informationPageText = CreateText("Số trang", noticeBox.transform, string.Empty, 15,
            new Color(.38f, .20f, .07f));
        SetAnchors(informationPageText.rectTransform, .69f, .055f, .84f, .135f);
        informationPageText.fontStyle = FontStyle.Normal;
        informationPageText.raycastTarget = false;
        Outline pageOutline = informationPageText.GetComponent<Outline>();
        if (pageOutline != null) pageOutline.enabled = false;

        GameObject nextObject = CreateTextureButton("Đọc trang tiếp", noticeBox.transform,
            LoadUiSprite("icon_arrow_down"), ShowNextInformationPage);
        RectTransform nextRect = nextObject.GetComponent<RectTransform>();
        nextRect.anchorMin = nextRect.anchorMax = new Vector2(.88f, .09f);
        nextRect.pivot = new Vector2(.5f, .5f);
        nextRect.anchoredPosition = Vector2.zero;
        nextRect.sizeDelta = new Vector2(48f, 40f);
        informationNextButton = nextObject.GetComponent<Button>();
        AddSoftShadow(nextObject, new Vector2(1.5f, -2f));

        GameObject closeObject = CreateTextureButton("Đóng hướng dẫn", noticeBox.transform,
            LoadUiSprite("cancel"), CloseInformation);
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(.94f, .91f);
        closeRect.pivot = new Vector2(.5f, .5f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = new Vector2(45f, 49f);
        AddSoftShadow(closeObject, new Vector2(1.5f, -2f));

        informationOverlay.SetActive(false);
    }

    private void BuildMap9HudGuide(Transform parent)
    {
        map9HudGuide = new GameObject("Minh họa HUD Màn 9", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        map9HudGuide.transform.SetParent(parent, false);
        Center(map9HudGuide.GetComponent<RectTransform>(), new Vector2(400f, 108f),
            new Vector2(0f, 66f));
        Image panel = map9HudGuide.GetComponent<Image>();
        panel.sprite = Resources.Load<Sprite>("Sprites/Map9_Art/ui/map9_circuit_hud");
        panel.preserveAspect = true;
        panel.raycastTarget = false;

        map9HudGuideMeter = CreateImage("Timeline HUD", map9HudGuide.transform,
            Resources.Load<Sprite>("Sprites/Map9_Art/ui/map9_circuit_hud_timeline"));
        Stretch(map9HudGuideMeter.rectTransform);
        map9HudGuideMeter.color = Color.white;
        map9HudGuideMeter.preserveAspect = false;
        map9HudGuideMeter.type = Image.Type.Filled;
        map9HudGuideMeter.fillMethod = Image.FillMethod.Horizontal;
        map9HudGuideMeter.fillOrigin = (int)Image.OriginHorizontal.Left;
        map9HudGuideMeter.raycastTarget = false;

        map9HudGuideStatus = CreateImage("Trạng thái HUD", map9HudGuide.transform, null);
        RectTransform statusRect = map9HudGuideStatus.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0f, .5f);
        statusRect.pivot = new Vector2(.5f, .5f);
        statusRect.anchoredPosition = new Vector2(59f, 2f);
        statusRect.sizeDelta = new Vector2(58f, 58f);
        map9HudGuideStatus.raycastTarget = false;

        map9HudGuideTitle = CreateText("Tiêu đề HUD", map9HudGuide.transform, string.Empty, 18,
            new Color(.35f, 1f, .95f));
        SetAnchors(map9HudGuideTitle.rectTransform, .25f, .52f, .91f, .82f);
        map9HudGuideTitle.raycastTarget = false;

        map9HudGuideDetail = CreateText("Chi tiết HUD", map9HudGuide.transform, string.Empty, 14,
            new Color(.95f, .92f, .68f));
        SetAnchors(map9HudGuideDetail.rectTransform, .25f, .26f, .91f, .55f);
        map9HudGuideDetail.raycastTarget = false;

        map9HudGuide.SetActive(false);
    }

    private void OpenInformation()
    {
        if (animating || isInformationOpen || isPaused) return;
        isInformationOpen = true;
        informationPage = 0;
        pauseButton.SetActive(false);
        informationButton.SetActive(false);
        informationOverlay.SetActive(true);
        informationGroup.alpha = 1f;

        if (!NetSession.IsOnline)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        RefreshInformationPage();
        PlayClick();
    }

    private void CloseInformation()
    {
        if (!isInformationOpen) return;
        isInformationOpen = false;
        informationOverlay.SetActive(false);
        pauseButton.SetActive(true);
        informationButton.SetActive(true);
        if (!NetSession.IsOnline)
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
        }
        PlayClick();
    }

    private void ShowNextInformationPage()
    {
        string[] pages = CurrentInformationPages();
        if (pages.Length == 0) return;
        informationPage = (informationPage + 1) % pages.Length;
        RefreshInformationPage();
        PlayClick();
    }

    private void RefreshInformationPage()
    {
        string[] pages = CurrentInformationPages();
        informationPage = Mathf.Clamp(informationPage, 0, Mathf.Max(0, pages.Length - 1));
        informationTitle.text = GameManagement.levelData != null
            ? GameManagement.levelData.levelName.ToUpperInvariant()
            : "THÔNG TIN MÀN CHƠI";
        informationBody.text = pages.Length > 0 ? pages[informationPage] :
            "Thông tin cho màn chơi này đang được cập nhật.";
        informationPageText.text = pages.Length > 1
            ? "TRANG " + (informationPage + 1) + "/" + pages.Length
            : string.Empty;
        informationNextButton.gameObject.SetActive(pages.Length > 1);
        RefreshMap9HudGuide();
    }

    private void RefreshMap9HudGuide()
    {
        bool isMap9 = GameManagement.levelData != null
            && GameManagement.levelData.level == Map9LevelIndex;
        map9HudGuide.SetActive(isMap9);
        SetAnchors(informationBody.rectTransform, .095f, .145f, .905f, isMap9 ? .52f : .785f);
        if (!isMap9) return;

        int page = Mathf.Clamp(informationPage, 0, Map9GuideStatusNames.Length - 1);
        map9HudGuideStatus.sprite = Resources.Load<Sprite>(
            "Sprites/Map9_Art/ui/map9_status_" + Map9GuideStatusNames[page]);
        map9HudGuideTitle.text = Map9GuideTitles[page];
        map9HudGuideDetail.text = Map9GuideDetails[page];
        map9HudGuideMeter.fillAmount = Map9GuideProgress[page];
    }

    private static string[] CurrentInformationPages()
    {
        int level = GameManagement.levelData != null ? GameManagement.levelData.level : -1;
        if (level < 0 || level >= LevelInformationPages.Length)
            return new[] { "Thông tin cho màn chơi này đang được cập nhật." };
        return LevelInformationPages[level][0];
    }

    private void OnDestroy()
    {
        RestoreGameState();
        if (instance == this) instance = null;
    }

    private void CreateMenuRow(Transform parent, string label, Vector2 position, bool firstStyle,
        string iconName, UnityEngine.Events.UnityAction action)
    {
        GameObject button = CreateTextureButton(label, parent,
            LoadUiSprite(firstStyle ? "menu_child1" : "menu_child2"), action);
        Center(button.GetComponent<RectTransform>(), firstStyle
            ? new Vector2(286f, 114f)
            : new Vector2(286f, 110f), position);
        AddSoftShadow(button, new Vector2(2f, -3f));

        Text text = CreateText("Chữ " + label, button.transform, label, 22, Color.white);
        SetAnchors(text.rectTransform, .29f, .22f, .82f, .78f);
        text.raycastTarget = false;

        Image icon = CreateImage("Icon " + iconName, button.transform, LoadUiSprite(iconName));
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.86f, .5f);
        icon.rectTransform.pivot = new Vector2(.5f, .5f);
        icon.rectTransform.sizeDelta = new Vector2(43f, 47f);
        icon.rectTransform.anchoredPosition = Vector2.zero;
        icon.raycastTarget = false;
    }

    private void CreateSoundRow(Transform parent, Vector2 position, bool firstStyle)
    {
        GameObject button = CreateTextureButton("Âm thanh", parent,
            LoadUiSprite(firstStyle ? "menu_child1" : "menu_child2"), ToggleSound);
        Center(button.GetComponent<RectTransform>(), new Vector2(286f, 110f), position);
        AddSoftShadow(button, new Vector2(2f, -3f));

        Text text = CreateText("Chữ Âm thanh", button.transform, "ÂM THANH", 21, Color.white);
        SetAnchors(text.rectTransform, .28f, .22f, .67f, .78f);
        text.raycastTarget = false;

        soundStateImage = CreateImage("Trạng thái âm thanh", button.transform,
            LoadUiSprite(UiAudioSettings.Enabled ? "on" : "off"));
        soundStateImage.rectTransform.anchorMin = soundStateImage.rectTransform.anchorMax = new Vector2(.81f, .5f);
        soundStateImage.rectTransform.pivot = new Vector2(.5f, .5f);
        soundStateImage.rectTransform.sizeDelta = new Vector2(53f, 33f);
        soundStateImage.rectTransform.anchoredPosition = Vector2.zero;
        soundStateImage.raycastTarget = false;
    }

    private static GameObject CreateTextureButton(string name, Transform parent, Sprite sprite,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(PauseTextureButton));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        PauseTextureButton motion = go.GetComponent<PauseTextureButton>();
        motion.target = image;
        return go;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 13;
        text.resizeTextMaxSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(.13f, .055f, .012f, .92f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private void PlayClick()
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/UI/buttonClick");
        if (clip != null) uiAudio.PlayOneShot(clip);
    }

    private static Sprite LoadUiSprite(string name) => Resources.Load<Sprite>("GameUI/" + name);

    private static void AddSoftShadow(GameObject target, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .48f);
        shadow.effectDistance = distance;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

/// <summary>Lưu lựa chọn âm thanh dùng chung qua các scene.</summary>
public static class UiAudioSettings
{
    private const string SoundPrefKey = "game_audio_enabled";
    public static bool Enabled => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

    public static void ApplySavedState()
    {
        AudioListener.volume = Enabled ? 1f : 0f;
    }

    public static bool Toggle()
    {
        bool enabled = !Enabled;
        PlayerPrefs.SetInt(SoundPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = enabled ? 1f : 0f;
        return enabled;
    }
}

public class PauseTextureButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public Image target;
    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 16f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = Vector3.one * 1.035f;
    public void OnPointerExit(PointerEventData eventData) => targetScale = Vector3.one;
    public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * .95f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = Vector3.one * 1.035f;
}
