using UnityEngine;
using MoreMountains.Feedbacks;
public class ManaStone : MonoBehaviour
{
    public MMFeedbacks fb;
    private void Update()
    {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 1))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                Player.instance.IncreaseStat("Mana", 100);
                fb.PlayFeedbacks();
                Destroy(this.gameObject);
                break;
            }
        }
    }
}
