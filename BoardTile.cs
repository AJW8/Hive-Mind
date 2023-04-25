using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardTile : MonoBehaviour
{
    public Board board;
    public int x;
    public int y;
    public int z;
    public int colour;
    private int pColour;
    private Vector2 oldPosition;
    private Vector2 newPosition;
    private float transition;
    private float defaultScale;
    private float selectedScale;
    private Quaternion origin;
    private bool check;
    private bool selected;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer backSprite;
    public Color[] colours;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        backSprite = transform.Find("Back Sprite").GetComponent<SpriteRenderer>();
        pColour = colour;
        transition = 0;
        defaultScale = transform.localScale.x;
        selectedScale = transform.localScale.x;
        origin = transform.rotation;
        check = false;
        selected = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transition > 0)
        {
            transition -= Time.deltaTime;
            if (colour != pColour) transform.Rotate(Mathf.PI * 2 / board.transitionTime, 0, 0);
            if (transition <= 0)
            {
                transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
                transform.rotation = origin;
                pColour = colour;
                spriteRenderer.color = colours[colour];
                if (board.GetCompleted())
                {
                    selected = false;
                    transform.localScale = new Vector3(defaultScale, defaultScale, transform.localScale.z);
                    return;
                }
                board.SetActive(true);
            }
            else
            {
                float f = transition / board.transitionTime;
                Vector2 currentPosition = newPosition + (oldPosition - newPosition) * f;
                transform.position = new Vector3(currentPosition.x, currentPosition.y, transform.position.z);
                Color c = spriteRenderer.color;
                c.a = f;
                spriteRenderer.color = c;
            }
        }
        float scale = selected ? selectedScale : defaultScale;
        if (transform.localScale.x != scale)
        {
            bool b = transform.localScale.x < scale;
            float f = Mathf.Abs(defaultScale - selectedScale) * Time.deltaTime / board.transitionTime;
            transform.localScale += new Vector3(f, f, 0) * (b ? 1 : -1);
            if (b ? transform.localScale.x > scale : transform.localScale.x < scale) transform.localScale = new Vector3(scale, scale, transform.localScale.z);
        }
    }

    /// <summary>
    /// Set the tile scale for when it is not selected.
    /// </summary>
    /// <param name="s">The new scale of the tile when not selected.</param>
    public void SetDefaultScale(float s)
    {
        defaultScale = s;
        transform.localScale = new Vector3(selected ? selectedScale : defaultScale, selected ? selectedScale : defaultScale, transform.localScale.z);
    }

    /// <summary>
    /// Return the check status of the tile.
    /// </summary>
    /// <returns>Whether the tile has been checked in the current group evaluation.</returns>
    public bool GetChecked()
    {
        return check;
    }

    /// <summary>
    /// Return the current group status of the tile.
    /// </summary>
    /// <param name="c">Whether ths tile is currently marked as grouped.</param>
    public void CheckGroup(int c)
    {
        if (check || colour != c) return;
        check = true;
        List<BoardTile> adjacent = board.GetAdjacentTiles(this);
        foreach (BoardTile t in adjacent) t.CheckGroup(c);
    }

    /// <summary>
    /// Set the check status of the tile to false.
    /// </summary>
    public void Uncheck()
    {
        check = false;
    }

    /// <summary>
    /// Set the tile scale for when it is selected.
    /// </summary>
    /// <param name="s">The new scale of the tile when selected.</param>
    public void SetSelectedScale(float s)
    {
        selectedScale = s;
        transform.localScale = new Vector3(selected ? selectedScale : defaultScale, selected ? selectedScale : defaultScale, transform.localScale.z);
    }

    /// <summary>
    /// Set the selected status of the tile.
    /// </summary>
    /// <param name="s">Whether the tile should be marked as currently selected.</param>
    public void SetSelected(bool s)
    {
        selected = s;
    }

    /// <summary>
    /// Set the tile to transition to its new position/colour.
    /// </summary>
    public void SetTransition()
    {
        board.SetActive(false);
        backSprite.color = colours[colour];
        oldPosition = new Vector2(transform.position.x, transform.position.y);
        newPosition = board.GetPosition(x, y, z);
        transition = board.transitionTime;
    }
}
