using System;
using UnityEditor;
using UnityEngine;

public static class Map9ArtValidation
{
    private const string Root = "Sprites/Map9_Art/";

    [MenuItem("Tools/Validation/Validate Map 9 Art")]
    public static void RunBatch()
    {
        int checkedCount = 0;
        Check("background/map9", 1916, 821, ref checkedCount);
        Check("nodes/map9_node_dormant", 256, 256, ref checkedCount);
        Check("nodes/map9_node_target", 256, 256, ref checkedCount);
        Check("nodes/map9_node_charged", 256, 256, ref checkedCount);
        Check("nodes/map9_node_overload", 256, 256, ref checkedCount);
        Check("links/map9_energy_link", 512, 64, ref checkedCount);
        for (int i = 0; i < 12; i++)
            Check("vfx/success/map9_success_" + i.ToString("00"), 256, 256, ref checkedCount);
        for (int i = 0; i < 8; i++)
            Check("vfx/overload/map9_overload_" + i.ToString("00"), 256, 256, ref checkedCount);
        Check("vfx/status/map9_zombie_lightning_hit", 256, 256, ref checkedCount);
        Check("vfx/status/map9_zombie_frozen_aura", 256, 256, ref checkedCount);
        Check("vfx/status/map9_zombie_slow_aura", 256, 256, ref checkedCount);
        Check("vfx/status/map9_plant_overload_hit", 256, 256, ref checkedCount);
        Check("ui/map9_circuit_hud", 1017, 288, ref checkedCount);
        Check("ui/map9_circuit_hud_timeline", 1017, 288, ref checkedCount);
        Check("ui/map9_status_waiting", 256, 256, ref checkedCount);
        Check("ui/map9_status_active", 256, 256, ref checkedCount);
        Check("ui/map9_status_success", 256, 256, ref checkedCount);
        Check("ui/map9_status_failure", 256, 256, ref checkedCount);
        Debug.Log("Map 9 art validation passed: " + checkedCount + "/36 runtime sprites loaded.");
    }

    private static void Check(string relativePath, int expectedWidth, int expectedHeight,
        ref int checkedCount)
    {
        Sprite sprite = Resources.Load<Sprite>(Root + relativePath);
        if (sprite == null)
            throw new InvalidOperationException("Missing Map 9 Sprite resource: " + relativePath);
        if (sprite.texture.width != expectedWidth || sprite.texture.height != expectedHeight)
            throw new InvalidOperationException("Unexpected Map 9 Sprite size for " + relativePath
                + ": " + sprite.texture.width + "x" + sprite.texture.height + ", expected "
                + expectedWidth + "x" + expectedHeight + ".");
        checkedCount++;
    }
}
