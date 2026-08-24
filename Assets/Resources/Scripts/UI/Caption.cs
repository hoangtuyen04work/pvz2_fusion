using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caption : MonoBehaviour
{
    Queue<CaptionNode> captionQueue = new Queue<CaptionNode>();   //Hàng đợi phụ đề chờ hiển thị
    SpriteRenderer spriteRenderer;   //Component SpriteRenderer của chính nó
    AudioSource audioSource;   //Component AudioSource của chính nó

    //Animation thu nhỏ phụ đề
    bool haveShrinked = false;  //Phụ đề đã thu nhỏ xong chưa
    Vector3 shrinkVelocity = new Vector3(1000, 1000, 0);  //Tốc độ thu nhỏ phụ đề
    Vector3 captionMaxScale = new Vector3(200, 200, 0);   //Tỉ lệ lớn nhất của phụ đề
    int x_MinScale = 80;   //Thành phần x của tỉ lệ nhỏ nhất của phụ đề

    //Hiển thị phụ đề đứng yên
    bool isShowing = false;   //Có đang hiển thị phụ đề không
    float showTimer = 0;    //Bộ đếm thời gian hiển thị phụ đề

    CaptionNode nowNode;  //Phụ đề đang hiển thị hiện tại

    // Start is called before the first frame update
    void Start()
    {
        //Đứng yên lúc đầu, phòng khi FPS thấp lúc mới bắt đầu làm hỏng hiệu ứng hiển thị
        gameObject.SetActive(false);
        Invoke("activate", 2f);

        //Lấy component
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        audioSource = GetComponent<AudioSource>();

        showGameStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (isShowing == true)
        {
            updateCaption();
        }
        if (isShowing == false && captionQueue.Count > 0)
        {
            changeCaption();
        }
    }

    private void activate()
    {
        gameObject.SetActive(true);
    }

    //Cập nhật trạng thái phụ đề
    private void updateCaption()
    {
        if (haveShrinked == false)
        {
            transform.localScale -= shrinkVelocity * Time.deltaTime;
            if (transform.localScale.x <= x_MinScale)
            {
                haveShrinked = true;
            }
        }
        else
        {
            showTimer += Time.deltaTime;
            if (showTimer > nowNode.showTime)
            {
                isShowing = false;
                spriteRenderer.enabled = false;
            }
        }
    }

    //Đổi phụ đề đang hiển thị
    private void changeCaption()
    {
        nowNode = captionQueue.Dequeue();

        //Cập nhật ảnh
        spriteRenderer.sprite =
                    Resources.Load<Sprite>("Sprites/UI/Caption/" + nowNode.caption);
        transform.localScale = captionMaxScale;
        spriteRenderer.enabled = true;
        isShowing = true;
        haveShrinked = false;
        showTimer = 0;

        //Phát âm thanh
        audioSource.clip = 
            Resources.Load<AudioClip>("Sounds/UI/Caption/" + nowNode.caption);
        audioSource.Play();
    }

    //Chuẩn bị hiện phụ đề bắt đầu trò chơi
    public void showGameStart()
    {
        captionQueue.Enqueue(new CaptionNode("StartReady", 0.5f));
        captionQueue.Enqueue(new CaptionNode("StartSet", 0.5f));
        captionQueue.Enqueue(new CaptionNode("StartPlant", 0.75f));
    }

    //Chuẩn bị hiện phụ đề một đợt zombie lớn
    public void showWave()
    {
        captionQueue.Enqueue(new CaptionNode("HugeWave", 3f));
    }

    //Chuẩn bị hiện phụ đề đợt cuối cùng
    public void showFinalWave()
    {
        captionQueue.Enqueue(new CaptionNode("HugeWave", 3f));
        captionQueue.Enqueue(new CaptionNode("FinalWave", 3f));
    }
}

public class CaptionNode
{
    public string caption;
    public float showTime;

    public CaptionNode(string caption, float showTime)
    {
        this.caption = caption;
        this.showTime = showTime;
    }
}