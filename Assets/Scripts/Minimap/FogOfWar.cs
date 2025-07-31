using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class FogOfWar : MonoBehaviour
{
    public Tilemap tilemap;
    public Tile tile;
    public Player player;
    public int radius;

    // Start is called before the first frame update
    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        player = Player.instance;
        FillMap(new Vector3Int(-100, -100, 0), new Vector3Int(100, 100, 0));
    }

    // Update is called once per frame
    void Update()
    {
        Vector3Int mid = new Vector3Int((int) player.transform.position.x, (int) player.transform.position.y, (int) player.transform.position.z) ;

        for (int x = - radius; x < radius; x++) {
            for (int y = - radius; y < radius; y++) {
                Vector3Int total = mid + new Vector3Int(x, y, 0);
                tilemap.SetTile(total, null);
            }
        }

    }

    void FillMap(Vector3Int leftBot, Vector3Int rightTop) {
        int width = rightTop.x - leftBot.x;
        int height = rightTop.y - leftBot.y;
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++) {
                tilemap.SetTile(leftBot + new Vector3Int(i, j, 0), tile);
            }
        }
    }

}
