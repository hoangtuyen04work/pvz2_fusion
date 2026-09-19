using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectZombieRegion : MonoBehaviour
{
    public GameObject myPlant;
    public BoxCollider2D myCollider;
    private readonly List<Zombie> zombies = new List<Zombie>();

    private void Start()
    {
        float rightEdge = 5.3f;
        float leftEdge = myPlant.transform.position.x;
        myCollider.size = new Vector2(rightEdge - leftEdge, myCollider.size.y);
        myCollider.offset = new Vector2((rightEdge - leftEdge) / 2, 0);
        myCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Zombie zombie = collision.GetComponent<Zombie>();
        if(collision.tag == "Zombie" && zombie != null && !zombie.IsHypnotized &&
            zombie.pos_row == myPlant.GetComponent<Plant>().row && !zombies.Contains(zombie))
            zombies.Add(zombie);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Zombie zombie = collision.GetComponent<Zombie>();
        if (zombie != null) zombies.Remove(zombie);
    }

    private void Update()
    {
        for(int i=zombies.Count-1;i>=0;i--)
            if(zombies[i]==null || zombies[i].IsHypnotized || !zombies[i].gameObject.activeInHierarchy)
                zombies.RemoveAt(i);
        myPlant.GetComponent<Animator>().SetBool("Attack", zombies.Count>0);
    }
}
