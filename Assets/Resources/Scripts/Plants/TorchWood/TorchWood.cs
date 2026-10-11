using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cây Liễu Đuốc.
public class TorchWood : Plant
{
    public GameObject firePea;
    public int firePeaHurt = 30;
    public Collider2D burnRegionCollider;

    int zombieNum = 0; //�ڻ�����˷�Χ�ڵĽ�ʬ����
    List<Collider2D> zombies = new List<Collider2D>();  //�����˷�Χ�ڵĽ�ʬ�б�
    ContactFilter2D contactFilter = new ContactFilter2D();  //��ײ��̽�������������̽�⽩ʬ

    protected override void Start()
    {
        base.Start();

        warm();

        contactFilter = ContactFilter2D.noFilter;
        contactFilter.SetLayerMask(LayerMask.GetMask("Zombie"));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ImportedProjectile imported = collision.GetComponent<ImportedProjectile>();
        if (imported != null)
        {
            imported.PassThroughTorchwood(row, firePeaHurt, this);
            return;
        }
        if(collision.tag == "Pea")
        {
            StraightBullet pea = collision.GetComponent<StraightBullet>();
            if (pea == null || pea.Row != row) return;
            //���ɻ��㶹
            Instantiate(firePea,
                        collision.transform.position,
                        Quaternion.Euler(0, 0, 0))
                .GetComponent<StraightBullet>().initialize(row, firePeaHurt);
            //�����㶹
            Destroy(collision.gameObject);
        }
        else if(collision.tag == "Zombie" && !collision.GetComponent<Zombie>().IsHypnotized && collision.GetComponent<Zombie>().pos_row == row)
        {
            zombieNum++;
            if(zombieNum == 1)
            {
                InvokeRepeating("burnZombie", 0.0f, 1.0f);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Zombie" && !collision.GetComponent<Zombie>().IsHypnotized && collision.GetComponent<Zombie>().pos_row == row)
        {
            zombieNum--;
            if (zombieNum <= 0)
            {
                CancelInvoke();
            }
        }
    }

    private void burnZombie()
    {
        int eligible=0;
        if(burnRegionCollider.Overlap(contactFilter, zombies) != 0)
        {
            foreach(Collider2D collider in zombies)
            {
                Zombie zombie=collider.GetComponent<Zombie>();
                if (zombie!=null && !zombie.IsHypnotized && zombie.pos_row == row)
                {
                    eligible++;
                    zombie.beBurned();
                }
            }
        }
        zombieNum=eligible;
        if(eligible==0)
        {
            CancelInvoke();
        }
    }

    protected override void beforeDie()
    {
        Transform region = transform.Find("WarmPlantRegion");
        if (region == null) return;
        WarmPlantRegion warmRegion = region.GetComponent<WarmPlantRegion>();
        if (warmRegion != null) warmRegion.stopWarm();
    }

    protected override void intensify_specific()
    {
        GetComponent<Animator>().speed = 1.5f;
        firePeaHurt = (int)(firePeaHurt * 1.5);
    }

    protected override void cancelIntensify_specific()
    {
        GetComponent<Animator>().speed = 1f;
        firePeaHurt = (int)(firePeaHurt / 1.5);
    }
}
