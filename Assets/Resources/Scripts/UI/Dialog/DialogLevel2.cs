using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLevel2 : MonoBehaviour
{
    public SpeechBubble flowerSpeechBubble;   //Khung thoại của Hướng Dương
    public SpeechBubble nutSpeechBubble;    //Khung thoại của Tường Hạt Dẻ
    public SpeechBubble peaSpeechBubble;   //Khung thoại của Đậu Bắn
    public GameObject introduce;   //Bảng giới thiệu zombie

    GameObject flower;
    GameObject nut;
    GameObject pea;

    BGMusicControl bGMusicControl;   //Component nguồn âm thanh của đối tượng nền

    int count = 0;  //Đếm hội thoại, hiện là câu thứ mấy

    private void Awake()
    {
        //Lấy component
        bGMusicControl = GameObject.Find("Background").GetComponent<BGMusicControl>();
        //Trồng những cây tham gia hội thoại
        flower = GameObject.Find("Plant-0-2")
            .GetComponent<PlantGrid>().plantByGod("SunFlowerForDialog");
        nut = GameObject.Find("Plant-4-2")
            .GetComponent<PlantGrid>().plantByGod("WallNut");
    }

    // Start is called before the first frame update
    void Start()
    {
        //Đặt nhạc nền
        bGMusicControl.changeMusic("Music_Night");

        flowerSpeechBubble.showDialog("Cuối cùng cũng tìm được cậu, Hạ sĩ Hạt Dẻ!");
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
                    nutSpeechBubble.showDialog("Lâu rồi không gặp, Chỉ huy vẫn khỏe chứ?");
                    count++;
                    break;
                case 1:
                    flowerSpeechBubble.showDialog("Cũng ổn, chỉ là đứng mãi trên đống gạch này khó chịu thật");
                    count++;
                    break;
                case 2:
                    flowerSpeechBubble.showDialog("Giá mà có Hạ sĩ Chậu Hoa ở đây thì tốt");
                    count++;
                    break;
                case 3:
                    nutSpeechBubble.showDialog("Tùy cảnh mà an lòng");
                    count++;
                    break;
                case 4:
                    peaSpeechBubble.showDialog("Nguy rồi! Nguy rồi!");
                    count++;
                    break;
                case 5:
                    flowerSpeechBubble.showDialog("Ồn ào cái gì, không thấy ta đang hàn huyên với Hạ sĩ Hạt Dẻ à");
                    count++;
                    break;
                case 6:
                    pea = GameObject.Find("Plant-8-2")
                        .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
                    peaSpeechBubble.gameObject.GetComponent<RectTransform>().localPosition -=
                        new Vector3(85, 0, 0);
                    peaSpeechBubble.showDialog("Đừng tán gẫu nữa, bên ngoài đầy zombie kỳ lạ, sắp tràn vào rồi");
                    count++;
                    break;
                case 7:
                    flowerSpeechBubble.showDialog("Cái gì! Đêm hôm thế này, Chỉ huy Nấm Mặt Trời lại không có ở đây, phen này xong đời");
                    count++;
                    break;
                case 8:
                    nutSpeechBubble.showDialog("Chỉ huy chớ lo âu, giữa cõi kỳ diệu này, đêm cũng có vầng trăng sáng ngàn dặm");
                    count++;
                    break;
                case 9:
                    peaSpeechBubble.showDialog("Ngàn dặm gì chứ? Anh Hạt Dẻ này, sao tự dưng ăn nói văn vẻ thế");
                    count++;
                    break;
                case 10:
                    flowerSpeechBubble.showDialog("Thôi kệ, mọi người vào vị trí, ta là Chỉ huy Hướng Dương chứ đâu phải hữu danh vô thực");
                    count++;
                    break;
                case 11:
                    introduce.SetActive(true);

                    //Cây hội thoại biến mất
                    flower.GetComponent<Plant>().die("");
                    nut.GetComponent<Plant>().die("");
                    pea.GetComponent<Plant>().die("");

                    count++;
                    break;
                default:
                    break;
            }
        }
    }

    public void clickStart()
    {
        AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>("Sounds/UI/graveButtonClick"),
                new Vector3(0, 0, -10)
            );
        introduce.SetActive(false);

        //Đổi nhạc nền
        bGMusicControl.changeMusicSmoothly("Music_Night_Wall");

        GameObject.Find("Game Management").GetComponent<GameManagement>().awakeAll();
        gameObject.SetActive(false);
    }
}
