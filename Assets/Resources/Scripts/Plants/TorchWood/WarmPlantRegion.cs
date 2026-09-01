using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarmPlantRegion : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Plant"))
        {
            Plant plant = collision.GetComponent<Plant>();
            if (plant != null) plant.warm();
        }
    }

    public void stopWarm()
    {
        List<Collider2D> plants = new List<Collider2D>();

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        contactFilter.SetLayerMask(LayerMask.GetMask("Plant"));

        if (GetComponent<Collider2D>().Overlap(contactFilter, plants) != 0)
        {
            HashSet<Plant> warmedPlants = new HashSet<Plant>();
            foreach (Collider2D collider in plants)
            {
                if (collider == null) continue;
                Plant plant = collider.GetComponent<Plant>();
                if (plant != null && warmedPlants.Add(plant)) plant.stopWarm();
            }
        }
    }
}
