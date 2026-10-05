using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndMenu : MonoBehaviour
{
    public Text dialogText;   //Component Text của đối tượng con DialogText, dùng để cập nhật font
    public AudioSource backgroundAudio;   //Component phát nhạc nền
    private Image resultArtwork;
    private RectTransform resultRect;
    private CanvasGroup resultGroup;
    private bool presentationBuilt;

    //Zombie chạm vạch: phe cây thua, phe zombie trong chế độ đối kháng thì thắng
    public void gameOver()
    {
        bool localWins = NetSession.ControlsZombies;
        show(localWins, localWins
            ? "Zombie của bạn đã ăn được não!"
            : "Zombie đã ăn mất não bạn");
    }

    public void win()
    {
        Invoke("win_real", 5f);
    }

    private void win_real()
    {
        bool localWins = !NetSession.ControlsZombies;
        show(localWins, localWins
            ? "Bạn đã đẩy lùi được lũ zombie"
            : "Hàng cây đã cầm cự tới cùng, bạn thua");
    }

    private void show(bool localWins, string message)
    {
        //Hiển thị giao diện
        Time.timeScale = 0;
        BuildPresentation();
        Sprite artwork = LoadResultSprite(localWins
            ? "Prefabs/UI/winner"
            : "Prefabs/UI/over");
        resultArtwork.sprite = artwork;
        resultArtwork.preserveAspect = true;
        if (artwork != null && artwork.rect.height > 0f)
            FitResultArtwork(artwork);

        // Text cũ chỉ còn là phương án dự phòng nếu asset bị thiếu.
        if (dialogText != null)
        {
            dialogText.text = message;
            dialogText.gameObject.SetActive(artwork == null);
        }
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(RevealPresentation());

        //Phát âm thanh
        backgroundAudio.Stop();
        GetComponent<AudioSource>().clip =
            Resources.Load<AudioClip>(localWins ? "Sounds/UI/winMusic" : "Sounds/UI/loseMusic");
        GetComponent<AudioSource>().Play();
    }

    private static Sprite LoadResultSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;
        Sprite[] importedSprites = Resources.LoadAll<Sprite>(resourcePath);
        return importedSprites != null && importedSprites.Length > 0 ? importedSprites[0] : null;
    }

    private void BuildPresentation()
    {
        if (presentationBuilt) return;
        presentationBuilt = true;

        // HUD gameplay và pause dùng Canvas riêng; kết quả phải nằm trên tất cả.
        Canvas resultCanvas = GetComponent<Canvas>();
        if (resultCanvas == null) resultCanvas = gameObject.AddComponent<Canvas>();
        resultCanvas.overrideSorting = true;
        resultCanvas.sortingOrder = 2000;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        // Loại bỏ toàn bộ khung kết quả cũ nhưng giữ reference để fallback khi thiếu ảnh.
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        Image backdrop = GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.sprite = null;
            backdrop.color = new Color(0f, 0f, 0f, .78f);
            backdrop.raycastTarget = true;
        }

        resultGroup = GetComponent<CanvasGroup>();
        if (resultGroup == null) resultGroup = gameObject.AddComponent<CanvasGroup>();

        var artworkObject = new GameObject("Result Artwork", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        artworkObject.transform.SetParent(transform, false);
        resultArtwork = artworkObject.GetComponent<Image>();
        resultArtwork.raycastTarget = false;
        resultRect = artworkObject.GetComponent<RectTransform>();
        Center(resultRect, new Vector2(280f, 190f), new Vector2(0f, 76f));

        // Chỉ dùng hai icon gọn bên dưới banner, không thêm bảng hay chữ.
        CreateResultIcon("Chơi lại", "confirm", new Vector2(-43f, -89f), restartLevel);
        CreateResultIcon("Về menu", "return", new Vector2(43f, -89f), exitGame);
    }

    private void FitResultArtwork(Sprite artwork)
    {
        const float maxWidth = 280f;
        const float maxHeight = 190f;
        float aspect = artwork.rect.width / artwork.rect.height;
        float width = maxWidth;
        float height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }
        resultRect.sizeDelta = new Vector2(width, height);
    }

    private void CreateResultIcon(string objectName, string iconName, Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(PauseTextureButton), typeof(Shadow));
        buttonObject.transform.SetParent(transform, false);
        Center(buttonObject.GetComponent<RectTransform>(), new Vector2(58f, 63f), position);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = Resources.Load<Sprite>("GameUI/" + iconName);
        buttonImage.preserveAspect = true;
        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);
        buttonObject.GetComponent<PauseTextureButton>().target = buttonImage;
        Shadow shadow = buttonObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .48f);
        shadow.effectDistance = new Vector2(2f, -3f);
    }

    private IEnumerator RevealPresentation()
    {
        resultGroup.alpha = 0f;
        resultRect.localScale = Vector3.one * .86f;
        float elapsed = 0f;
        const float duration = .38f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            resultGroup.alpha = progress;
            resultRect.localScale = Vector3.one * Mathf.Lerp(.86f, 1f, progress);
            yield return null;
        }
        resultGroup.alpha = 1f;
        resultRect.localScale = Vector3.one;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void restartLevel()
    {
        RestoreTime();
        SceneManager.LoadScene("GameScene");
    }

    private void RestoreTime()
    {
        CancelInvoke();
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void exitGame()
    {
        // Nút "Kết thúc" của bảng kết quả phải quay về menu, không thoát ứng dụng.
        // Khôi phục thời gian trước khi đổi scene vì bảng kết quả đã đặt timeScale = 0.
        RestoreTime();
        SceneManager.LoadScene("MainMenu");
    }
}
