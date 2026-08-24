using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLevel3 : MonoBehaviour
{
    public SpeechBubble flowerSpeechBubble;   //Khung thoại của Hướng Dương
    public SpeechBubble squashSpeechBubble;    //Khung thoại của Bí Ngòi
    public SpeechBubble peaSpeechBubble;   //Khung thoại của Đậu Bắn
    public GameObject introduce1;   //Bảng giới thiệu zombie 1
    public GameObject introduce2;   //Bảng giới thiệu zombie 2

    GameObject flower;
    GameObject squash;
    GameObject pea;

    int count = 0;  //Đếm hội thoại, hiện là câu thứ mấy

    private void Awake()
    {
        //Trồng những cây tham gia hội thoại
        flower = GameObject.Find("Plant-2-2")
            .GetComponent<PlantGrid>().plantByGod("SunFlowerForDialog");
        pea = GameObject.Find("Plant-5-2")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
    }

    // Start is called before the first frame update
    void Start()
    {
        Invoke("flipPea", 1f);
        Invoke("flipPea", 2f);
        Invoke("peaHide", 3f);
        Invoke("showFirstTalk", 3.5f);
    }

    // Update is called once per frame
    void Update()
    {
        //Bấm chuột trái để sang sự kiện tiếp theo
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            switch (count)
            {
                case 1:
                    flowerSpeechBubble.showDialog("Đúng là vậy thật");
                    count++;
                    break;
                case 2:
                    flowerSpeechBubble.showDialog("Nhưng Hạ sĩ Đậu Bắn này, cậu không thấy núp sau lưng Chỉ huy là một nỗi nhục sao");
                    count++;
                    break;
                case 3:
                    peaSpeechBubble.showDialog("Ờ......");
                    count++;
                    break;
                case 4:
                    squash = GameObject.Find("Plant-5-2")
                        .GetComponent<PlantGrid>().plantByGod("SquashForDialog");
                    Invoke("squashTalk", 1f);
                    count = -1;
                    break;
                case 5:
                    peaSpeechBubble.showDialog("Thiếu úy Bí Ngòi!!!");
                    count++;
                    break;
                case 6:
                    squashSpeechBubble.showDialog("Là chiến binh, chúng ta phải xông thẳng về phía trước");
                    count++;
                    break;
                case 7:
                    pea.GetComponent<Plant>().die("");
                    pea = GameObject.Find("Plant-6-2")
                        .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
                    Invoke("peaTalk2", 0.5f);
                    count = -1;
                    break;
                case 8:
                    GameObject.Find("Zombie Management").GetComponent<ZombieManagement>()
                        .createZombieByGod("Ghost", 3);
                    Invoke("peaAmazed", 1f);
                    count = -1;
                    break;
                case 9:
                    squashSpeechBubble.showDialog("Chuyện bé xé ra to");
                    count++;
                    break;
                case 10:
                    introduce1.SetActive(true);

                    //Cây hội thoại biến mất
                    flower.GetComponent<Plant>().die("");
                    squash.GetComponent<Plant>().die("");
                    pea.GetComponent<Plant>().die("");

                    count++;
                    break;
                default:
                    break;
            }
        }
    }

    private void flipPea()
    {
        pea.GetComponent<SpriteRenderer>().flipX = !(pea.GetComponent<SpriteRenderer>().flipX);
    }

    private void showFirstTalk()
    {
        peaSpeechBubble.showDialog("Chỉ huy Hướng Dương ơi, chỗ này âm u quá");
        count++;
    }

    private void peaTalk2()
    {
        peaSpeechBubble.transform.localPosition += new Vector3(300, 0, 0);
        peaSpeechBubble.showDialog("Cậu nói đúng, là chiến binh, tôi nhất định sẽ chắn phía trước cấp trên");
        count = 8;
    }

    private void peaAmazed()
    {
        Instantiate(
            Resources.Load<GameObject>("Prefabs/UI/Effect/Effect_Amazed"),
            pea.transform.position + new Vector3(0.32f, 0.57f, 0),
            Quaternion.Euler(0, 0, 0),
            pea.transform
        );
        Invoke("peaHide", 1f);
    }

    private void peaHide()
    {
        pea.GetComponent<Plant>().die("");
        pea = GameObject.Find("Plant-1-2")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
        if(count == -1)
        {
            peaSpeechBubble.transform.localPosition -= new Vector3(300, 0, 0);
            peaSpeechBubble.showDialog("Ối *! Cái quái gì thế kia");
            count = 9;
        }
            
    }

    private void squashTalk()
    {
        squashSpeechBubble.showDialog("Chỉ huy Hướng Dương nói đúng");
        count = 5;
    }

    public void clickNext()
    {
        introduce1.SetActive(false);
        introduce2.SetActive(true);
    }

    public void clickStart()
    {
        AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>("Sounds/UI/graveButtonClick"),
                new Vector3(0, 0, -10)
            );
        introduce2.SetActive(false);

        GameObject.Find("Game Management").GetComponent<GameManagement>().awakeAll();
        gameObject.SetActive(false);
    }
}
