
public class Key : DroppedItem
{

    public enum KeyType
    {
        BRONZE, SILVER, GOLD
    }

    public KeyType keyType; // Type of key.

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnCollect()
    {
        switch (keyType)
        {
            case KeyType.BRONZE:
            Player.instance.IncreaseStatUnclamp("Bronze Key Collected", 1);
            break;

            case KeyType.SILVER:
            Player.instance.IncreaseStatUnclamp("Silver Key Collected", 1);
            break;

            case KeyType.GOLD:
            Player.instance.IncreaseStatUnclamp("Gold Key Collected", 1);
            break;
        }

        SFXLibrary.instance.CollectingKeySFX.PlayFeedbacks();
        Inventory.instance.AddKey(keyType);
    }
}
