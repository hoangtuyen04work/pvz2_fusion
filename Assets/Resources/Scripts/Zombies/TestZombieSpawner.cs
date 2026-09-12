using UnityEngine;
using UnityEngine.EventSystems;
public class TestZombieSpawner : MonoBehaviour
{
    ZombieManagement manager; int selected; string imported;
    void Start() { manager = GetComponent<ZombieManagement>(); }
    void Update()
    {
        for (int i = 0; i < 9; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { selected = i; imported = null; }
        if (Input.GetKeyDown(KeyCode.F1)) imported = "FlagZombie";
        if (Input.GetKeyDown(KeyCode.F2)) imported = "NewspaperZombie";
        if (!Input.GetMouseButtonDown(1) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        foreach (Collider2D hit in Physics2D.OverlapPointAll(p)) { PlantGrid grid = hit.GetComponent<PlantGrid>(); if (grid != null && (imported != null ? manager.SpawnImportedTestZombie(imported, grid.row) : manager.SpawnTestZombie(selected, grid.row))) return; }
    }
}
