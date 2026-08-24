using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieManagement : MonoBehaviour
{
    //Liên quan tới zombie
    public GameObject[] zombies;   //Danh sách zombie có thể sinh ra
    Dictionary<string, int> zombiesName = new Dictionary<string, int>();   //Từ điển ánh xạ tên zombie sang chỉ số đối tượng

    //Liên quan tới số lượng zombie
    int zombieNum_now = 0;   //Số zombie hiện có trên màn, dùng để đảm bảo hết sạch zombie rồi mới sinh đợt mới

    //Liên quan tới việc sinh zombie
    List<int> rowList = new List<int>();  //Danh sách hàng có thể sinh zombie
                                          //Sinh ở hàng nào thì loại hàng đó ra, tránh việc zombie cứ xuất hiện mãi ở một hàng
    public string generateFunc = "generateZombies";  //Tên hàm sinh zombie, tiện gọi hàm sinh riêng của các màn đặc biệt

    //Vị trí ban đầu của zombie
    float initPos_x = 6.0f;

    //Liên quan tới trục thời gian
    TimeNodes timeNodes;   //Danh sách mốc thời gian đọc từ json
    int nowNode_index = 0;   //Hiện đang ở mốc thời gian thứ mấy
    int nodeCount = 0;  //Tổng cộng có bao nhiêu mốc thời gian
    TimeNode nowNode;   //Mốc thời gian hiện tại
    bool waitWave = false;   //Có đang chờ sinh một đợt zombie không
    bool isOver = false;   //Màn chơi đã kết thúc chưa
    DecreasingSlider flagMeter;   //Component thanh tiến trình màn chơi

    //Liên quan tới phụ đề
    public Caption caption;  //Component phụ đề

    AudioSource audioSource;   //Component AudioSource của chính nó

    private void Awake()
    {
        //Lấy đối tượng và component
        audioSource = GetComponent<AudioSource>();
        flagMeter = GameObject.Find("FlagMeter-Slider").GetComponent<DecreasingSlider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Đọc file json của màn chơi và chuyển thành đối tượng biến
        string info = Resources.Load<TextAsset>(
            "Json/ZombieData/Level" + GameManagement.levelData.level
        ).text;
        timeNodes = JsonUtility.FromJson<TimeNodes>(info);

        //Khởi tạo danh sách tên zombie có thể sinh ra
        for(int i = 0; i < zombies.Length; i++)
        {
            zombiesName.Add(zombies[i].name, i);
        }

        //Khởi tạo phần liên quan tới sinh zombie
        initRowList();

        //Khởi tạo phần liên quan tới trục thời gian
        nodeCount = timeNodes.info.Count;
        nowNode_index = 0;
        nowNode = timeNodes.info[nowNode_index];
    }

    public void activate()
    {
        //Chuẩn bị đợt đầu tiên
        Invoke("enterTimeNode", nowNode.deltaTime);
    }

    private void enterTimeNode()
    {
        if (nowNode.isWave == false)   //Không phải một đợt lớn
        {
            Invoke(generateFunc, 0);
        }
        else   //Là một đợt lớn
        {
            if (zombieNum_now == 0)   //Hết sạch zombie trên màn rồi mới sinh một đợt lớn
            {
                waitWave = false;
                if(nowNode.isFinalWave == false) caption.showWave();
                else caption.showFinalWave();
                Invoke(generateFunc, 0);
            }
            else waitWave = true;
        }
    }

    //Sinh lần lượt toàn bộ zombie của mốc thời gian hiện tại
    //Hàm này không phải không được dùng, mà do gọi bằng Invoke nên trình biên dịch không nhận ra
    private void generateZombies()
    {
        for (int i = 0; i < nowNode.number; i++)
        {
            //Lấy hàng ngẫu nhiên
            int randY = rowList[Random.Range(0, rowList.Count)];
            rowList.Remove(randY);
            if (rowList.Count == 0) initRowList();
            //Sinh zombie
            GameObject newZombie = Instantiate(zombies[zombiesName[nowNode.zombie]],
                new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[randY], 0),
                Quaternion.Euler(0, 0, 0),
                transform);
            newZombie.GetComponent<Zombie>().setPosRow(randY);
            addZombieNumAll();
        }
        changeTimeNode();
    }

    //Chuyển sang mốc thời gian kế tiếp và cập nhật tương ứng
    private void changeTimeNode()
    {
        //Nếu vừa sinh ra là lứa zombie đầu tiên
        if(nowNode_index == 0)
        {
            audioSource.Play(); //Phát âm thanh báo động
            InvokeRepeating("groan", 0, 5);   //Cứ mỗi 5 giây chạy hàm phát âm thanh zombie thở dài
        }

        //Chuyển mốc thời gian
        nowNode_index++;
        if (nowNode_index < nodeCount)   //Nếu còn mốc thời gian phía sau thì chuyển
        {
            nowNode = timeNodes.info[nowNode_index];
            Invoke("enterTimeNode", nowNode.deltaTime);
        }
        else   //Hết rồi thì kết thúc trò chơi
        {
            isOver = true;
        }

        //Cập nhật thanh tiến trình màn chơi
        flagMeter.setValue((nodeCount - nowNode_index) / (float)nodeCount);
    }

    //Tạo zombie ở chế độ god mode, dùng cho hội thoại
    public void createZombieByGod(string name, int posRow)
    {
        GameObject newZombie = Instantiate(zombies[zombiesName[name]],
            new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[posRow], 0),
            Quaternion.Euler(0, 0, 0),
            transform);
        newZombie.GetComponent<Zombie>().setPosRow(posRow);
        newZombie.GetComponent<Zombie>().cancelSleep();
        addZombieNumAll();
    }

    //Khởi tạo danh sách hàng có thể sinh zombie
    private void initRowList()
    {
        for (int i = 0; i < GameManagement.levelData.landRowCount; i++)
        {
            rowList.Add(i);
        }
    }

    private void groan()
    {
        int rand = Random.Range(1, 50);
        if(rand <= 6)
        {
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Sounds/Zombies/groan" + rand));
        }
    }

    public void addZombieNumAll()
    {
        zombieNum_now++;
    }

    //Giảm một zombie trên màn
    public void minusZombieNumAll()
    {
        zombieNum_now--;  //Giảm một zombie trên màn

        //Nếu trên màn không còn zombie
        if (zombieNum_now <= 0)
        {
            //Nếu đang có một đợt zombie lớn chờ sinh thì sinh ra
            if(waitWave == true)
            {
                enterTimeNode();
            }
            //Nếu đã sinh hết zombie thì kết thúc trò chơi
            else if(isOver == true)
            {
                GameObject.Find("Game Management").GetComponent<GameManagement>().win();
            }
        }
    }

    #region Vùng hàm dành riêng cho màn đặc biệt

    //Hàm sinh zombie dành riêng cho màn 2
    //Hàm này không phải không được dùng, mà do gọi bằng Invoke nên trình biên dịch không nhận ra
    private void generateZombies_level2()
    {
        //Trong màn này nowNode.number là số đội zombie
        for (int i = 0; i < nowNode.number; i++)
        {
            int zombieNum = Random.Range(3, 6);  //Sinh ngẫu nhiên số zombie của đội này
            ChineseZombie last = null;  //Zombie trước đó

            //Lấy hàng ngẫu nhiên
            int randY = rowList[Random.Range(0, rowList.Count)];
            rowList.Remove(randY);
            if (rowList.Count == 0) initRowList();

            //Độ lệch toạ độ ngang ngẫu nhiên của đội zombie này
            float allOffset = Random.Range(0.0f, 2.0f);

            float offset = 0.85f;  //Độ lệch toạ độ ngang giữa các zombie

            for (int j = 0; j < zombieNum; j++)
            {
                //Sinh zombie
                GameObject newZombie = Instantiate(zombies[zombiesName[nowNode.zombie]],
                    new Vector3(
                        initPos_x + allOffset + j * offset,
                        GameManagement.levelData.zombieInitPosY[randY],
                        0
                    ),
                    Quaternion.Euler(0, 0, 0),
                    transform);
                newZombie.GetComponent<Zombie>().setPosRow(randY);
                addZombieNumAll();
                //Đặt thông tin danh sách liên kết của zombie
                if(last == null)
                {
                    last = newZombie.GetComponent<ChineseZombie>();
                    last.prior = null;
                    last.isCaptain = true;
                }
                else
                {
                    ChineseZombie newCZ = newZombie.GetComponent<ChineseZombie>();
                    last.next = newCZ;
                    newCZ.prior = last;
                    last = newCZ;
                }
            }
            last.next = null;
        }

        changeTimeNode();
    }

    //Dùng để tạo Bóng Ma ngẫu nhiên
    public void createGhost()
    {
        //Lấy hàng ngẫu nhiên
        int randY = Random.Range(0, GameManagement.levelData.rowCount);
        //Sinh Bóng Ma
        GameObject newZombie = Instantiate(zombies[zombiesName["Ghost"]],
            new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[randY], 0),
            Quaternion.Euler(0, 0, 0),
            transform);
        newZombie.GetComponent<Zombie>().setPosRow(randY);
        //Tạo lại sau một khoảng thời gian ngẫu nhiên
        if(nowNode_index <= 8)
            Invoke("createGhost", Random.Range(15.0f, 20.0f));
        else Invoke("createGhost", Random.Range(5.0f, 10.0f));
    }

    #endregion
}

[System.Serializable]
public class TimeNode
{
    public float deltaTime;  //Bao lâu nữa thì bắt đầu đợt tấn công tiếp theo
    public bool isWave;  //Có phải một đợt lớn không
    public bool isFinalWave;   //Có phải đợt cuối cùng không
    public int number;   //Số lượng zombie
    public string zombie;   //Tên zombie
}

[System.Serializable]
public class TimeNodes
{
    public List<TimeNode> info;
}