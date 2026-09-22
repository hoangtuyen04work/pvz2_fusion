using UnityEngine;

public enum AsetFaction
{
    Defender,
    Enemy
}

/// <summary>
/// Dữ liệu cân bằng tách khỏi MonoBehaviour để có thể chỉnh máu, tốc độ và sát thương
/// mà không phải sửa mã nguồn hoặc sửa từng prefab.
/// </summary>
[CreateAssetMenu(menuName = "PvZ/Aset/Entity Definition", fileName = "EntityDefinition")]
public sealed class AsetEntityDefinition : ScriptableObject
{
    [Header("Nhận dạng")]
    public string displayName = "Entity";
    public AsetFaction faction;

    [Header("Chỉ số")]
    [Min(1f)] public float maximumHealth = 100f;
    [Min(0f)] public float movementSpeed = 1.5f;
    [Min(0f)] public float attackDamage = 15f;
    [Min(0.1f)] public float attackRange = 1.2f;
    [Min(0.05f)] public float attackInterval = 1f;
}
