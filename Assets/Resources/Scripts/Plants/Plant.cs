using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    protected PlantGrid myGrid;   //Grid mà cây này đang đứng
    public int row;  //Cây này ở hàng thứ mấy

    public int bloodVolume;
    private int bloodVolumeMax;

    public PlantState state = PlantState.Normal;
    protected int warmSource = 0;  //Xung quanh có mấy nguồn sưởi ấm

    protected bool intensified = false;   //Có đang ở trạng thái tăng cường không

    protected AudioSource audioSource;   //Component AudioSource của chính nó

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    protected virtual void Start()
    {
        bloodVolumeMax = bloodVolume;
        if(SnowKingSong.play)
        {
            intensify();
        }
    }

    public virtual int beAttacked(int hurt, string form)
    {
        bloodVolume -= hurt;
        if (bloodVolume <= 0)
        {
            die(form);
        }
        return bloodVolume;
    }

    public virtual void cold()
    {
        if (state == PlantState.Normal && !intensified)
        {
            state = PlantState.Cold;
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Sounds/Plants/frozen"));
            GetComponent<SpriteRenderer>().color = new Color(0.33f, 0.54f, 1f);
            GetComponent<Animator>().speed = 0.5f;
            Invoke("coldHurt", 1f);
        }
    }

    protected virtual void coldHurt()
    {
        if(state == PlantState.Cold)
        {
            beAttacked(8, "coldHurt");
            Invoke("coldHurt", 1f);
        }
    }

    public virtual void warm()
    {
        warmSource++;
        if(warmSource == 1 && !intensified)
        {
            state = PlantState.Warm;
            GetComponent<SpriteRenderer>().color = Color.white;
            GetComponent<Animator>().speed = 1f;
        }
    }

    public virtual void stopWarm()
    {
        warmSource--;
        if (warmSource <= 0 && !intensified)
        {
            normal();
        }
    }

    public virtual void normal()
    {
        state = PlantState.Normal;
        GetComponent<SpriteRenderer>().color = Color.white;
        GetComponent<Animator>().speed = 1f;
    }

    public void recover(int value)
    {
        bloodVolume += value;
        if(bloodVolume > bloodVolumeMax) bloodVolume = bloodVolumeMax;
    }

    //Hàm tăng cường, chạy thao tác chung rồi gọi hàm thao tác riêng
    public void intensify()
    {
        if(!intensified)
        {
            intensified = true;
            normal();
            transform.Find("Halo").gameObject.SetActive(true);
            intensify_specific();
        }
    }

    //Thao tác tăng cường riêng
    protected virtual void intensify_specific()
    {
        GetComponent<Animator>().speed = 1.5f;
    }

    //Hàm huỷ tăng cường, chạy thao tác chung rồi gọi hàm thao tác riêng
    public void cancelIntensify()
    {
        if(intensified)
        {
            intensified = false;
            transform.Find("Halo").gameObject.SetActive(false);
            if (warmSource > 0) state = PlantState.Warm;
            else state = PlantState.Normal;
            cancelIntensify_specific();
        }
    }

    //Thao tác huỷ tăng cường riêng
    protected virtual void cancelIntensify_specific()
    {
        GetComponent<Animator>().speed = 1f;
    }

    public virtual void attack(bool attack)
    {

    }

    public virtual void highlight()
    {
        GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.75f, 0.75f);
    }

    public virtual void cancelHighlight()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    public virtual void initialize(PlantGrid grid, string sortingLayer, int sortingOrder)
    {
        GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
        GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        row = grid.row;
        myGrid = grid;
    }

    public void die(string reason)
    {
        beforeDie();
        myGrid.plantDie(reason);
        Destroy(gameObject);
    }

    // Loai cay khoi o de thay bang cay lai, khong danh dau o la trong.
    public void removeForFusion()
    {
        beforeDie();
        Destroy(gameObject);
    }

    //Việc cần xử lý trước khi chết, do từng loại cây tự cài đặt
    protected virtual void beforeDie()
    {

    }
}

public enum PlantState { Normal, Warm, Cold }
