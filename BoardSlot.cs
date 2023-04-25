using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public Board board;
    public BoardTile tile;
    private bool aligned;
    private bool selected;
    public bool grouped;
    private float transition;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer backSprite;
    private Color defaultColour;
    public Color alignedColour;
    public Color selectedColour;
    public Color groupedColour;

    void Awake()
    {
        aligned = false;
        selected = false;
        grouped = false;
        transition = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
        backSprite = transform.Find("Back Sprite").GetComponent<SpriteRenderer>();
        defaultColour = spriteRenderer.color;
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
            if (transition <= 0) spriteRenderer.color = grouped ? groupedColour : defaultColour;
            else
            {
                Color c = spriteRenderer.color;
                c.a = transition / board.transitionTime;
                spriteRenderer.color = c;
            }
        }
    }

    /// <summary>
    /// Return true if the slot is currently transitioning, false otherwise.
    /// </summary>
    /// <returns>Whether the slot is transitioning.</returns>
    public bool GetTransitioning()
    {
        return transition > 0;
    }

    /// <summary>
    /// Set the grouping status of the slot to the passed value.
    /// </summary>
    /// <param name="g">Whether the slot's tile should be marked as grouped with all tiles of the same colour.</param>
    public void SetGrouped(bool g)
    {
        grouped = g;
        spriteRenderer.color = selected ? selectedColour : aligned ? alignedColour : g ? groupedColour : defaultColour;
    }

    /// <summary>
    /// Set the alignment status of the slot to the passed value.
    /// </summary>
    /// <param name="a">Whether the slot's tile should be marked as aligned with a currently selected tile.</param>
    public void SetAligned(bool a)
    {
        aligned = a;
        spriteRenderer.color = selected ? selectedColour : a ? alignedColour : grouped ? groupedColour : defaultColour;
    }

    /// <summary>
    /// Set the selection status of the slot to the passed value.
    /// </summary>
    /// <param name="s">Whether the slot's respective tile should be marked as currently selected.</param>
    public void SetSelected(bool s)
    {
        selected = s;
        spriteRenderer.color = s ? selectedColour : aligned ? alignedColour : grouped ? groupedColour : defaultColour;
    }

    /// <summary>
    /// Set the slot to transition to its new position/colour.
    /// </summary>
    public void SetTransition()
    {
        backSprite.color = grouped ? groupedColour : defaultColour;
        transform.position = board.GetPosition(tile.x, tile.y, tile.z);
        transition = board.transitionTime;
    }
}
