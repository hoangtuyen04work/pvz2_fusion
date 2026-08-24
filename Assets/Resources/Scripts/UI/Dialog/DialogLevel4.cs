using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogLevel4 : MonoBehaviour
{
    public SpeechBubble peaSpeechBubble;   //Khung thoại của Đậu Bắn
    public SpeechBubble woodAndFlowerSpeechBubble;   //Khung thoại của Đuốc và Hướng Dương
    public SpeechBubble kingSpeechBubble;   //Khung thoại của Vua Tuyết
    public GameObject zombieIntroduce1;   //Bảng giới thiệu zombie 1
    public GameObject zombieIntroduce2;   //Bảng giới thiệu zombie 2
    public GameObject plantIntroduce;     //Bảng giới thiệu cây

    GameObject flower;
    GameObject pea;
    GameObject wood;
    GameObject snowKing;

    int count = 0;  //Đếm hội thoại, hiện là câu thứ mấy
    private void Awake()
    {
        //Trồng những cây tham gia hội thoại
        flower = GameObject.Find("Plant-0-3")
            .GetComponent<PlantGrid>().plantByGod("SunFlowerForDialog");
        pea = GameObject.Find("Plant-1-3")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
    }

    // Start is called before the first frame update
    void Start()
    {
        woodAndFlowerSpeechBubble.showDialog("Hắt......hắt xì!");
    }

    // Update is called once per frame
    void Update()
    {
        //Bấm chuột trái để sang sự kiện tiếp theo
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            switch (count)
            {
                case 0:
                    peaSpeechBubble.showDialog("Chỉ huy Hướng Dương, ngài ổn chứ......hắt xì!");
                    count++;
                    break;
                case 1:
                    flower.GetComponent<Plant>().cold();
                    Invoke("flowerLastWords", 2f);
                    count = -1;
                    break;
                case 2:
                    flower.GetComponent<Plant>().die("");
                    Invoke("peaShock", 0.5f);
                    count = -1;
                    break;
                case 3:
                    woodAndFlowerSpeechBubble.gameObject.GetComponent<Image>().sprite =
                        Resources.Load<Sprite>("Sprites/UI/SpeechBubble/SpeechBubble");
                    woodAndFlowerSpeechBubble.transform.localPosition += new Vector3(0, -71, 0);
                    woodAndFlowerSpeechBubble.showDialog("Không cần lo đâu");
                    count++;
                    break;
                case 4:
                    wood = GameObject.Find("Plant-0-3")
                        .GetComponent<PlantGrid>().plantByGod("TorchWood");
                    woodAndFlowerSpeechBubble.gameObject.GetComponent<Image>().sprite =
                        Resources.Load<Sprite>("Sprites/UI/SpeechBubble/SpeechBubble2");
                    woodAndFlowerSpeechBubble.transform.localPosition += new Vector3(0, 71, 0);
                    woodAndFlowerSpeechBubble.showDialog("Tôi đã đưa Chỉ huy vào hang nghỉ rồi");
                    count++;
                    break;
                case 5:
                    peaSpeechBubble.showDialog("Thiếu úy Đuốc! Gặp được cậu tốt quá!");
                    count++;
                    break;
                case 6:
                    peaSpeechBubble.showDialog("(hơ tay hơ tay)");
                    count++;
                    break;
                case 7:
                    GetComponent<AudioSource>().Play();
                    Invoke("woodPrepare", 1.5f);
                    count = -1;
                    break;
                case 8:
                    AudioSource.PlayClipAtPoint(
                        Resources.Load<AudioClip>("Sounds/Plants/MXBC"),
                        new Vector3(0, 0, -10)
                    );
                    Invoke("snowKingAppear", 1.5f);
                    count = -1;
                    break;
                case 9:
                    peaSpeechBubble.showDialog("Á!\nNgươi là ai?");
                    count++;
                    break;
                case 10:
                    woodAndFlowerSpeechBubble.showDialog("Đây là người bạn mới tôi quen ở đây, Vua Tuyết.");
                    count++;
                    break;
                case 11:
                    peaSpeechBubble.showDialog("Các cậu là.......bạn bè?");
                    count++;
                    break;
                case 12:
                    zombieIntroduce1.SetActive(true);
                    pea.GetComponent<Plant>().die("");
                    wood.GetComponent<Plant>().die("");
                    snowKing.GetComponent<Plant>().die("");
                    count++;
                    break;
                default:
                    break;
            }
        }
    }

    private void flowerLastWords()
    {
        woodAndFlowerSpeechBubble.showDialog("Đậu Bắn......hạ sĩ......ta......không xong rồi......cậu......nhất định phải......");
        count = 2;
    }

    private void peaShock()
    {
        peaSpeechBubble.showDialog("Chỉ huy Hướng Dương!");
        count = 3;
    }

    private void woodPrepare()
    {
        woodAndFlowerSpeechBubble.showDialog("Nguy rồi, lũ đó lại đến! Mọi người cẩn thận!");
        count = 8;
    }

    private void snowKingAppear()
    {
        kingSpeechBubble.showDialog("Đừng sợ, bản vương đến trợ giúp các ngươi một tay.");
        snowKing = GameObject.Find("Plant-0-2")
            .GetComponent<PlantGrid>().plantByGod("SnowKing");
        count = 9;
    }

    public void clickNext1()
    {
        zombieIntroduce1.SetActive(false);
        zombieIntroduce2.SetActive(true);
    }

    public void clickNext2()
    {
        zombieIntroduce2.SetActive(false);
        plantIntroduce.SetActive(true);
    }

    public void clickStart()
    {
        AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>("Sounds/UI/plantPanelAppear"),
                new Vector3(0, 0, -10)
            );
        plantIntroduce.SetActive(false);

        GameObject.Find("Game Management").GetComponent<GameManagement>().awakeAll();
        gameObject.SetActive(false);
    }

}
