using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpriteDebugger : MonoBehaviour
{
    public static SpriteDebugger instance;

    [Header("Components")]
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite circleSprite;

    private Dictionary<GameObject, float> destroyTimer = new Dictionary<GameObject, float>(); // Hashmap that contains all object.

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        List<GameObject> spriteObjectList = new List<GameObject>(destroyTimer.Keys);

        foreach (GameObject spriteObject in spriteObjectList)
        {
            if (spriteObject == null)
            {
                destroyTimer.Remove(spriteObject);
                continue;
            }

            if (destroyTimer[spriteObject] > 0f)
            {
                destroyTimer[spriteObject] -= Time.deltaTime;
            }
            else
            {
                destroyTimer.Remove(spriteObject);
                Destroy(spriteObject);
            }
        }
    }

    // Function to show Rectangle debugger.
    public GameObject CreateRectangle(Vector2 position, Vector2 size, float destroyTime)
    {
        GameObject spriteObject = new GameObject();
        spriteObject.transform.position = position;
        spriteObject.transform.localScale = size;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = new Color(1f, 0f, 0f, 0.25f);

        destroyTimer.Add(spriteObject, destroyTime);
        return spriteObject;
    }

    // Function to show Circle debugger.
    public GameObject CreateCircle(Vector2 position, float radius, float destroyTime)
    {
        GameObject spriteObject = new GameObject();
        spriteObject.transform.position = position;
        spriteObject.transform.localScale = Vector2.one * radius * 2f;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = circleSprite;
        spriteRenderer.color = new Color(1f, 0f, 0f, 0.25f);

        destroyTimer.Add(spriteObject, destroyTime);
        return spriteObject;
    }
}
