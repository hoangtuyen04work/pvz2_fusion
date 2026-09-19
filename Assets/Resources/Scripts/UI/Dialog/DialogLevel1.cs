using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLevel1 : MonoBehaviour
{
    public CrazyDave crazyDave;   //Component script của Dave Điên
    public SpeechBubble peaSpeechBubble;    //Khung thoại của Đậu Bắn
    public SpeechBubble daveSpeechBubble;   //Khung thoại của Dave Điên

    SpriteRenderer pea_spriteRenderer;   //SpriteRenderer của Đậu Bắn được sinh ra, dùng để lật nó

    int count = 0;  //Đếm hội thoại, hiện là câu thứ mấy

    private void Awake()
    {
        pea_spriteRenderer = GameObject.Find("Plant-4-2")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle")
            .GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Invoke("flipPea", 2f);
        Invoke("showFirstDialog", 3f);
    }

    // Update is called once per frame
    void Update()
    {
        //Bấm chuột trái để sang sự kiện tiếp theo
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            switch(count)
            {
                case 1:
                    crazyDave.gameObject.SetActive(true);
                    crazyDave.talk("Ôi~ máy cắt cỏ của tôi hỏng rồi");
                    count++;
                    break;
                case 2:
                    crazyDave.talk("You know......tôi không thể thiếu nó");
                    count++;
                    break;
                case 3:
                    peaSpeechBubble.showDialog("Nghe tôi nói này, cảm ơn cậu!");
                    count++;
                    break;
                case 4:
                    crazyDave.smallTalk("Không có gì đâu!");
                    count++;
                    break;
                case 5:
                    crazyDave.leave();
                    flipPea();
                    Invoke("showPeaDialog", 2f);
                    count = -1;   //Như vậy sau khi chạy câu lệnh tăng bên dưới thì count bằng 0
                                  //Lúc này bấm chuột không kích hoạt sự kiện, phải sau khi Invoke chạy mới sang sự kiện tiếp theo
                    break;
                case 6:
                    GameObject.Find("Game Management").GetComponent<GameManagement>().awakeAll();
                    gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
    }

    private void flipPea()
    {
        pea_spriteRenderer.flipX = !(pea_spriteRenderer.flipX);
    }

    private void showFirstDialog()
    {
        peaSpeechBubble.showDialog("Where is my xe đẩy!"); //Hiển thị hội thoại
        count++;   //Tăng bộ đếm
    }

    //Đây là câu đầu tiên của Đậu Bắn sau khi Dave rời đi
    private void showPeaDialog()
    {
        peaSpeechBubble.showDialog("Hừ~ đám tép riu này thì cần gì đến xe đẩy"); //Hiển thị hội thoại

        count = 6;   //Tăng bộ đếm
    }
}
