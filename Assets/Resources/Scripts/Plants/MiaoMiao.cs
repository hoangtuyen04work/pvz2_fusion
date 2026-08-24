using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiaoMiao : MultiImagePlant
{
    public GameObject parasiticSeed;   //Prefab hạt ký sinh
    public GameObject leafKnife;   //Prefab phi diệp đao
 
    public bool prepareParasitic = false;   //Có nên bắn hạt ký sinh không
    private Vector3 bulletOffset = new Vector3(0.054f, 0.218f, 0);   //Độ lệch vị trí ban đầu của đạn
    private Vector2 castEndPoint;   //Điểm cuối khi phóng raycast

    protected override void Start()
    {
        base.Start();
        castEndPoint = new Vector2(5.3f, transform.position.y);
    }

    public void fireEvent()
    {
        if(!prepareParasitic)
        {
            Instantiate(leafKnife,
                        transform.position + bulletOffset,
                        Quaternion.Euler(0, 0, 0))
                .GetComponent<LeafKnife>().initialize(this, row);
            audioSource.Play();
            return;
        }
        else
        {
            RaycastHit2D[] hitResults = 
                Physics2D.LinecastAll(transform.position, castEndPoint, LayerMask.GetMask("Zombie"));
            foreach(RaycastHit2D hitResult in hitResults)
            {
                if(hitResult.transform.GetComponent<Zombie>().pos_row == row)
                {
                    //Bắn hạt ký sinh
                    Instantiate(parasiticSeed,
                                transform.position + bulletOffset,
                                Quaternion.Euler(0, 0, 0))
                        .GetComponent<ParasiticSeed>()
                        .initialize(hitResult.transform.GetComponent<Zombie>(), this, row);
                    audioSource.Play();
                    prepareParasitic = false;
                    return;
                }
            }

            //Dò không thấy zombie, bắn phi diệp đao
            Instantiate(leafKnife,
                        transform.position + bulletOffset,
                        Quaternion.Euler(0, 0, 0))
                .GetComponent<LeafKnife>().initialize(this, row);
            audioSource.Play();
            return;
        }
        
    }

}
