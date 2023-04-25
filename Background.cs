using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public int columns;
    public int rows;
    public float tileScale;
    public float tileGap;
    private GameObject[] tiles;
    private float[] tileOpacity;
    public float tileSpeed;
    public float minTileOpacity;
    public float maxTileOpacity;
    private Vector2 boundary;
    private Vector2 maskScale;
    private bool darkTheme;
    public GameObject tilePrefab;

    // Start is called before the first frame update
    void Start()
    {
        tiles = new GameObject[columns * rows];
        tileOpacity = new float[columns * rows];
        float s = Mathf.Sin(Mathf.PI / 3);
        Transform mask = transform.Find("Sprite Mask");
        maskScale = new Vector2(mask.localScale.x, mask.localScale.y);
        darkTheme = PlayerPrefs.GetString("DarkTheme") == "On";
        mask.localScale = new Vector3(darkTheme ? 0 : maskScale.x, darkTheme ? 0 : maskScale.y, 1);
        SpriteRenderer sr = transform.Find("Background Fill 1").GetComponent<SpriteRenderer>();
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject tile = Instantiate(tilePrefab, transform.position + new Vector3(i + 1.25f - (j % 2 + columns) * 0.5f, (j + 0.5f - rows * 0.5f) * s, 0) * tileGap, transform.rotation);
                tile.transform.localScale = new Vector3(tileScale, tileScale, 1);
                tiles[i * rows + j] = tile;
                RandomiseTile(i * rows + j);
            }
        }
        boundary = new Vector2(columns * tileGap / 2, rows * tileGap * s / 2);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].transform.position += new Vector3(-tileSpeed / Mathf.Sqrt(3), tileSpeed, 0) * Time.deltaTime;
            if (tiles[i].transform.position.x < transform.position.x - boundary.x)
            {
                tiles[i].transform.position += new Vector3(boundary.x, 0, 0) * 2;
                RandomiseTile(i);
            }
            else if (tiles[i].transform.position.x > transform.position.x + boundary.x)
            {
                tiles[i].transform.position -= new Vector3(boundary.x, 0, 0) * 2;
                RandomiseTile(i);
            }
            if (tiles[i].transform.position.y < transform.position.y - boundary.y)
            {
                tiles[i].transform.position += new Vector3((rows % 2 == 0 ? 0 : tileGap * 0.25f), boundary.y, 0) * 2;
                RandomiseTile(i);
            }
            else if (tiles[i].transform.position.y > transform.position.y + boundary.y)
            {
                tiles[i].transform.position -= new Vector3((rows % 2 == 0 ? 0 : tileGap * 0.25f), boundary.y, 0) * 2;
                RandomiseTile(i);
            }
            if (darkTheme != (PlayerPrefs.GetString("DarkTheme") == "On"))
            {
                darkTheme = !darkTheme;
                transform.Find("Sprite Mask").localScale = new Vector3(darkTheme ? 0 : maskScale.x, darkTheme ? 0 : maskScale.y, 1);
            }
            SpriteRenderer sr = tiles[i].GetComponent<SpriteRenderer>();
            Color c = sr.color;
            Vector3 displacement = transform.position - tiles[i].transform.position;
            float a = tileOpacity[i] * Mathf.Sqrt(displacement.x * displacement.x + displacement.y * displacement.y) / Mathf.Sqrt(boundary.x * boundary.x + boundary.y * boundary.y);
            c.a = a;
            sr.color = c;
            sr = tiles[i].transform.Find("Background Tile 2").GetComponent<SpriteRenderer>();
            c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    /// <summary>
    /// Assign a random opacity to the tile with the passed index.
    /// </summary>
    /// <param name="index">The index of the tile to randomise.</param>
    private void RandomiseTile(int index)
    {
        tileOpacity[index] = Random.Range(minTileOpacity, maxTileOpacity);
    }
}
