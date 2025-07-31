using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMinimapGeneration : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private Tile roomTile;
    [SerializeField] private string roomOutlineLayer;

    [Header("References")]
    [SerializeField] private Transform grid;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private List<Camera> minimapCamera;
    [SerializeField] private Minimap minimap;

    private Vector2 center = Vector2.zero;
    private Vector2 size = Vector2.one;

    private void Start()
    {
        //Generate();
    }

    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.L))
        {
            Generate();
        }
        */
    }

    // Function to generate the tilemap.
    public void Generate()
    {
        // If the clone tilemap already existed, don't generate.
        if (grid.Find(this.tilemap.name + " (Minimap)"))
        {
            return;
        }

        GameObject gameObject = new GameObject(this.tilemap.name + " (Minimap)");
        gameObject.transform.SetParent(grid);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.layer = LayerMask.NameToLayer(roomOutlineLayer);

        Tilemap tilemap = gameObject.AddComponent<Tilemap>();
        gameObject.AddComponent<TilemapRenderer>();

        float minSizeX = float.MaxValue;
        float maxSizeX = float.MinValue;
        float minSizeY = float.MaxValue;
        float maxSizeY = float.MinValue;

        foreach (Vector3Int pos in this.tilemap.cellBounds.allPositionsWithin)
        {
            Vector3Int position = new Vector3Int(pos.x, pos.y, pos.z);

            if (this.tilemap.HasTile(position))
            {
                tilemap.SetTile(position, roomTile);
                Vector3 worldPosition = tilemap.CellToWorld(position);

                if (worldPosition.x < minSizeX)
                {
                    minSizeX = worldPosition.x;
                }

                if (worldPosition.y < minSizeY)
                {
                    minSizeY = worldPosition.y;
                }

                if (worldPosition.x > maxSizeX)
                {
                    maxSizeX = worldPosition.x;
                }

                if (worldPosition.y > maxSizeY)
                {
                    maxSizeY = worldPosition.y;
                }
            }
        }

        float centerX = (maxSizeX + minSizeX) / 2f;
        float centerY = (maxSizeY + minSizeY) / 2f;
        center = new Vector2(centerX, centerY);

        float sizeX = maxSizeX - minSizeX;
        float sizeY = maxSizeY - minSizeY;
        size = new Vector2(sizeX, sizeY);

        // Camera size 1 = 4 : 2 (+1 because size isn't perfect and need to extend a bit)

        foreach (Camera camera in minimapCamera)
        {
            if (sizeX > sizeY)
            {
                camera.orthographicSize = (sizeX / 2f);
            }
            else
            {
                camera.orthographicSize = sizeY;
            }

            if (camera.targetTexture)
            {
                RenderTexture rt = RenderTexture.active;
                RenderTexture.active = camera.targetTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = rt;
            }
        }

        minimapCamera[0].transform.parent.transform.position = new Vector3(center.x, center.y, minimapCamera[0].transform.parent.transform.position.z);
        minimap.ResetPosition(center);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
