using UnityEngine;

/// <summary>
/// Gắn lên mỗi zombie đang được đồng bộ.
/// Bên máy chủ chỉ dùng để nhớ id; bên máy khách thì kéo zombie về đúng vị trí máy chủ báo.
/// </summary>
public class NetZombieView : MonoBehaviour
{
    public int netId;

    private Vector3 target;
    private bool hasTarget;

    /// <summary>Vị trí mới nhất do máy chủ gửi tới.</summary>
    public void SetTarget(Vector3 value)
    {
        target = new Vector3(value.x, value.y, transform.position.z);
        hasTarget = true;
    }

    private void LateUpdate()
    {
        if (NetSession.IsAuthority || !hasTarget) return;

        float distance = Vector3.Distance(transform.position, target);
        if (distance > 1.5f)
        {
            //Lệch quá xa (mới sinh, hoặc vừa bị giật lag) thì nhảy thẳng cho đúng
            transform.position = target;
            return;
        }

        //Bám mượt theo vị trí máy chủ, không phụ thuộc tốc độ khung hình
        transform.position = Vector3.Lerp(
            transform.position, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
    }
}
