using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private bool active; // represents whether the board can currently be interacted with
    public int n; // the number of 'layers' that make up the board, with the base layer (n = 0) having 1 tile and each layer having 6 more tiles than the previous one
    private int pack; // the pack this board belongs to, which determines the types of move that can be made on this board (0 = shift, 1 = flip, 2 = spin, 3 = blink)
    private BoardTile[] tiles; // the list of all tiles that make up the board
    private BoardTile[] previewTiles; // the list of all tiles to be shown in preview mode (this is so the player can see what the board looks like when solved)

    // This game uses a custom coordinate system for tile positions, where each tile is assigned 3 coordinates. Each coordinate represents a 'dimension' along which all aligned tiles share the same value for that coordinate. This means no two tiles have more than one coordinate in common. The general rule for coordinate assignment is as follows:
    //
    //                   -n, n, 0            ....             0, n, n
    //
    //            ....              ....              ....              ....
    //
    // -n, 0,-n            ....             0, 0, 0            ....             n, 0, n
    //
    //            ....              ....              ....              ....
    //
    //                    0,-n,-n            ....             n,-n, 0

    private BoardSlot[] slots; // the list of all tile slots on the board
    private BoardSlot[] previewSlots; // the list of all tile slots to be shown in preview mode
    public float boardScale;
    // the following 4 variables represent the respective colour setup of the board in each pack before scrambling
    public string shiftSetup;
    public string flipSetup;
    public string spinSetup;
    public string blinkSetup;
    private int colours; // the number of tile colours making up this board
    // the following 4 variables represent the sequence of moves made on the original solved board in each pack - each string contains a sequence of moves separated by commas, while each move is represented by several ints separated by colons which represent the indices of the tiles the move involves (2 for shift and flip packs, 3 for spin and blink packs)
    public string shiftScramble;
    public string flipScramble;
    public string spinScramble;
    public string blinkScramble;
    private bool preview; // whether the board is currently in preview mode
    private MoveStack undoStack; // stores all previously made moves as part of the undo mechanic
    private MoveStack redoStack; // stores all previously undone moves as part of the redo mechanic - resets each time a new move is made
    private int tileIndex1; // the index of the first currently selected tile
    private int tileIndex2; // the index of the second currently selected tile (only used in spin and blink packs)
    private int moveCount; // the number of moves that have been made
    public float transitionTime; // the duration of tile transition each time a move is made (this is exclusively for visual effect as the states of tiles are changed as soon as the move is made)
    private bool completed; // whether the level is completed - checked after each move
    public GameObject tilePrefab;
    public GameObject slotPrefab;

    void Awake()
    {
        pack = PlayerPrefs.GetInt("SelectedPack");
        PlayerPrefs.SetInt("SelectedPack", -1);
    }

    // Start is called before the first frame update
    void Start()
    {
        string colourSetup = (new string[] { shiftSetup, flipSetup, spinSetup, blinkSetup })[pack];
        string scramble = (new string[] { shiftScramble, flipScramble, spinScramble, blinkScramble })[pack];
        active = true;
        int tileCount = n * 2 + 1;
        for (int i = n + 1; i < n * 2 + 1; i++) tileCount += i * 2;
        tiles = new BoardTile[tileCount];
        previewTiles = new BoardTile[tileCount];
        slots = new BoardSlot[tileCount];
        previewSlots = new BoardSlot[tileCount];
        int currentTile = 0;
        colours = 0;
        float s = Mathf.Sin(Mathf.PI / 3);
        for (int i = -n; i <= n; i++)
        {
            int x = (i < 0 ? -i : 0) - n;
            int z = (i < 0 ? 0 : i) - n;
            int l = n * 2 + 1 - Mathf.Abs(i);
            for (int j = 0; j < l; j++)
            {
                Vector2 position = GetPosition(x + j, i, z + j);
                int currentColour = int.Parse(colourSetup[currentTile] + "");
                GameObject[] tile = new GameObject[] { Instantiate(tilePrefab, transform.position + new Vector3(position.x, position.y, 0), transform.rotation), Instantiate(tilePrefab, transform.position + new Vector3(position.x, position.y, 0), transform.rotation) };
                GameObject[] slot = new GameObject[] { Instantiate(slotPrefab, transform.position + new Vector3(position.x, position.y, 0), transform.rotation), Instantiate(slotPrefab, transform.position + new Vector3(position.x, position.y, 0), transform.rotation) };
                for (int k = 0; k < 2; k++)
                {
                    BoardTile boardTile = tile[k].GetComponent<BoardTile>();
                    boardTile.board = this;
                    boardTile.x = x + j;
                    boardTile.y = i;
                    boardTile.z = z + j;
                    boardTile.colour = currentColour;
                    boardTile.SetDefaultScale(boardScale * 0.4f / (n * 2 + 1));
                    boardTile.SetSelectedScale(boardScale * 0.3f / (n * 2 + 1));
                    boardTile.SetTransition();
                    ((new BoardTile[][] { tiles, previewTiles })[k])[currentTile] = boardTile;
                    slot[k].transform.localScale = new Vector3(1, 1, 0) * boardScale * 0.475f / (n * 2 + 1) + new Vector3(0, 0, 1);
                    BoardSlot boardSlot = slot[k].GetComponent<BoardSlot>();
                    boardSlot.board = this;
                    boardSlot.tile = tile[0].GetComponent<BoardTile>();
                    if (k == 1) boardSlot.SetGrouped(true);
                    boardSlot.SetTransition();
                    ((new BoardSlot[][] { slots, previewSlots })[k])[currentTile] = boardSlot;
                }
                tile[1].GetComponent<SpriteRenderer>().sortingOrder++;
                slot[1].GetComponent<SpriteRenderer>().sortingOrder++;
                currentTile++;
                if (currentColour >= colours) colours = currentColour + 1;
            }
        }
        preview = false;
        undoStack = transform.Find("Move Stack Undo").GetComponent<MoveStack>();
        redoStack = transform.Find("Move Stack Redo").GetComponent<MoveStack>();
        bool[] colourUsed = new bool[colours];
        int[] newColours = new int[colours];
        int colourShift = Random.Range(0, colours);
        for (int i = 0; i < colours; i++)
        {
            if (pack < 0 || pack > 2) newColours[i] = (i + colourShift) % colours;
            else
            {
                int r;
                do r = Random.Range(0, colours);
                while (colourUsed[r]);
                newColours[i] = r;
                colourUsed[r] = true;
            }
        }
        for (int i = 0; i < tileCount; i++)
        {
            int newColour = newColours[tiles[i].colour];
            tiles[i].colour = newColour;
            previewTiles[i].colour = newColour;
        }
        moveCount = 0;
        if (scramble.Length > 0)
        {
            string[] moves = scramble.Split(',');
            for (int i = 0; i < moves.Length; i++)
            {
                string[] currentMove = moves[i].Split(':');
                Move(int.Parse(currentMove[0]), int.Parse(currentMove[1]), currentMove.Length == 3 ? int.Parse(currentMove[2]) : 0, true);
                //redoStack.Push(int.Parse(currentMove[0]), int.Parse(currentMove[1]), currentMove.Length == 3 ? int.Parse(currentMove[2]) : 0);
            }
        }
        moveCount = 0;
        tileIndex1 = -1;
        tileIndex2 = -1;
        CheckGrouped();
        foreach (BoardSlot slot in slots) slot.SetTransition();
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;
        Vector3 mouse = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));
        int hoverIndex = -1;
        for (int i = 0; i < tiles.Length; i++)
        {
            Vector3 displacement = mouse - slots[i].gameObject.transform.position;
            if (Mathf.Sqrt(displacement.x * displacement.x + displacement.y * displacement.y) < boardScale * 1.2f / (n * 2 + 1))
            {
                hoverIndex = i;
                i = tiles.Length;
            }
        }
        if (preview && hoverIndex >= 0) SetPreview(false);
        if (Input.GetMouseButtonDown(0)) // left click
        {
            if (tileIndex1 < 0) tileIndex1 = hoverIndex;
            else if (pack == 0 || pack == 1)
            {
                if (hoverIndex != tileIndex1)
                {
                    //Debug.Log((pack == 1 ? tileIndex1 : hoverIndex) + ":" + (pack == 1 ? hoverIndex : tileIndex1));
                    while (!redoStack.Empty()) redoStack.Pop();
                    redoStack.Push(tileIndex1, hoverIndex, 0);
                    Redo();
                }
                tileIndex1 = -1;
            }
            else if (hoverIndex < 0)
            {
                tileIndex1 = -1;
                tileIndex2 = -1;
            }
            else if (hoverIndex == tileIndex1)
            {
                if (tileIndex2 < 0) tileIndex1 = -1;
                else
                {
                    tileIndex1 = tileIndex2;
                    tileIndex2 = -1;
                }
            }
            else if (tiles[hoverIndex].x != tiles[tileIndex1].x && tiles[hoverIndex].y != tiles[tileIndex1].y && tiles[hoverIndex].z != tiles[tileIndex1].z)
            {
                tileIndex1 = -1;
                tileIndex2 = -1;
            }
            else if (tileIndex2 < 0) tileIndex2 = hoverIndex;
            else if (hoverIndex == tileIndex2) tileIndex2 = -1;
            else
            {
                //Debug.Log((pack == 2 ? tileIndex2 : tileIndex1) + ":" + (pack == 2 ? tileIndex1 : tileIndex2) + ":" + hoverIndex);
                while (!redoStack.Empty()) redoStack.Pop();
                redoStack.Push(tileIndex1, tileIndex2, hoverIndex);
                Redo();
            }
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            slots[i].SetAligned(hoverIndex < 0 ? false : tileIndex1 < 0 ? Alignment(i, hoverIndex) != ' ' : tileIndex2 < 0 ? (Alignment(i, hoverIndex) != ' ' && Alignment(i, hoverIndex) == Alignment(i, tileIndex1)) : FormTriangle(hoverIndex, tileIndex1, tileIndex2) ? InTriangle(hoverIndex, tileIndex1, tileIndex2, i, pack == 2) : FormTriangle(i, tileIndex1, tileIndex2));
            slots[i].SetSelected(i == hoverIndex || i == tileIndex1 || i == tileIndex2);
            tiles[i].SetSelected(i == hoverIndex || i == tileIndex1 || i == tileIndex2);
        }
    }

    /// <summary>
    /// Set whether the board is in preview mode.
    /// </summary>
    /// <param name="p">The new preview status of the board.</param>
    public void SetPreview(bool p)
    {
        preview = p;
        foreach (BoardTile t in previewTiles) t.gameObject.SetActive(p);
        foreach (BoardSlot s in previewSlots) s.gameObject.SetActive(p);
    }

    /// <summary>
    /// Undo the most recently made move.
    /// </summary>
    public void Undo()
    {
        tileIndex1 = -1;
        tileIndex2 = -1;
        if (undoStack.Empty()) return;
        int[] move = undoStack.Pop();
        if (Move(move[0], move[1], move[2], true)) redoStack.Push(move[0], move[1], move[2]);
    }

    /// <summary>
    /// Redo the most recently undone move.
    /// </summary>
    public void Redo()
    {
        tileIndex1 = -1;
        tileIndex2 = -1;
        if (redoStack.Empty()) return;
        int[] move = redoStack.Pop();
        if (Move(move[0], move[1], move[2], false)) undoStack.Push(move[0], move[1], move[2]);
    }

    /// <summary>
    /// Perform a move using the tiles with the passed indices.
    /// </summary>
    /// <param name="index1">The index of the first tile.</param>
    /// <param name="index2">The index of the second tile.</param>
    /// <param name="index3">The index in the third tile (only used for the spin/blink packs).</param>
    /// <param name="undo">True if the move is being undone, false otherwise.</param>
    /// <returns>True if the move is valid, false otherwise.</returns>
    private bool Move(int index1, int index2, int index3, bool undo)
    {
        if (index1 == index2) return false;
        else if (undo && (pack == 0 || pack == 2)) // undoing a shift/spin is the same as performing one with the first two indices reversed
        {
            int temp = index1;
            index1 = index2;
            index2 = temp;
        }
        if (pack == 0 || pack == 1) // shift/flip
        {
            char alignment = Alignment(index1, index2);
            if (alignment == ' ') return false;
            int[] indices = new int[n * 2 + 1];
            int length = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (i == index1 || i == index2 || (pack == 0 ? alignment == Alignment(i, index1) : Between(index1, index2, i)))
                {
                    indices[length] = i;
                    length++;
                }
            }
            int[] x = new int[length];
            int[] y = new int[length];
            int[] z = new int[length];
            int[] newIndices = new int[length];
            for (int i = 0; i < length; i++)
            {
                int min = n + 1;
                int index = 0;
                for (int j = 0; j < length; j++)
                {
                    if (indices[j] >= 0)
                    {
                        int value = alignment == 'x' ? tiles[indices[j]].y : tiles[indices[j]].x;
                        if (value < min)
                        {
                            min = value;
                            index = j;
                        }
                    }
                }
                if (alignment != 'x') x[i] = tiles[indices[index]].x;
                if (alignment != 'y') y[i] = tiles[indices[index]].y;
                if (alignment != 'z') z[i] = tiles[indices[index]].z;
                newIndices[i] = indices[index];
                indices[index] = -1;
            }
            indices = newIndices;
            int shift = alignment == 'x' ? tiles[index2].y - tiles[index1].y : tiles[index2].x - tiles[index1].x;
            for (int i = 0; i < length; i++)
            {
                int newIndex = pack == 0 ? (i + shift + length) % length : length - 1 - i;
                if (alignment != 'x') tiles[indices[i]].x = x[newIndex];
                if (alignment != 'y') tiles[indices[i]].y = y[newIndex];
                if (alignment != 'z') tiles[indices[i]].z = z[newIndex];
            }
        }
        else // spin/blink
        {
            if (!FormTriangle(index1, index2, index3)) return false;
            else if (pack == 2) // spin
            {
                char alignment1 = Alignment(index1, index2);
                char alignment2 = Alignment(index1, index3);
                char alignment3 = Alignment(index2, index3);
                int shift = Mathf.Abs(alignment1 == 'x' ? tiles[index1].y - tiles[index2].y : tiles[index1].x - tiles[index2].x);
                int length = Mathf.Abs(shift);
                int[] indices = new int[length * 3];
                int[] x = new int[length * 3];
                int[] y = new int[length * 3];
                int[] z = new int[length * 3];
                int[] startX = new int[] { tiles[index1].x, tiles[index2].x, tiles[index3].x };
                int[] startY = new int[] { tiles[index1].y, tiles[index2].y, tiles[index3].y };
                int[] startZ = new int[] { tiles[index1].z, tiles[index2].z, tiles[index3].z };
                int[] shiftX = new int[] { tiles[index1].x < tiles[index2].x ? 1 : tiles[index1].x > tiles[index2].x ? -1 : 0, tiles[index2].x < tiles[index3].x ? 1 : tiles[index2].x > tiles[index3].x ? -1 : 0, tiles[index1].x > tiles[index3].x ? 1 : tiles[index1].x < tiles[index3].x ? -1 : 0 };
                int[] shiftY = new int[] { tiles[index1].y < tiles[index2].y ? 1 : tiles[index1].y > tiles[index2].y ? -1 : 0, tiles[index2].y < tiles[index3].y ? 1 : tiles[index2].y > tiles[index3].y ? -1 : 0, tiles[index1].y > tiles[index3].y ? 1 : tiles[index1].y < tiles[index3].y ? -1 : 0 };
                int[] shiftZ = new int[] { tiles[index1].z < tiles[index2].z ? 1 : tiles[index1].z > tiles[index2].z ? -1 : 0, tiles[index2].z < tiles[index3].z ? 1 : tiles[index2].z > tiles[index3].z ? -1 : 0, tiles[index1].z > tiles[index3].z ? 1 : tiles[index1].z < tiles[index3].z ? -1 : 0 };
                for (int i = 0; i < indices.Length; i++)
                {
                    x[i] = startX[i / length] + shiftX[i / length] * (i % length);
                    y[i] = startY[i / length] + shiftY[i / length] * (i % length);
                    z[i] = startZ[i / length] + shiftZ[i / length] * (i % length);
                    for (int j = 0; j < tiles.Length; j++) if (tiles[j].x == x[i] && tiles[j].y == y[i] && tiles[j].z == z[i]) indices[i] = j;
                }
                for (int i = 0; i < indices.Length; i++)
                {
                    int newIndex = (i + shift + indices.Length) % indices.Length;
                    tiles[indices[i]].x = x[newIndex];
                    tiles[indices[i]].y = y[newIndex];
                    tiles[indices[i]].z = z[newIndex];
                }
            }
            else // blink
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (InTriangle(index1, index2, index3, i, false))
                    {
                        tiles[i].colour += (undo ? colours - 1 : 1);
                        tiles[i].colour %= colours;
                    }
                }
            }
        }
        CheckGrouped();
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].SetTransition();
            slots[i].SetTransition();
        }
        if (undo && moveCount > 0) moveCount--;
        else if (!undo) moveCount++;
        return true;
    }

    /// <summary>
    /// Evaluate the board to see if every colour is grouped together and set the completion status to the result.
    /// </summary>
    private void CheckGrouped()
    {
        completed = true;
        if (pack >= 0 && pack <= 2) // shift/flip/spin
        {
            bool[] grouped = new bool[colours];
            for (int i = 0; i < colours; i++)
            {
                grouped[i] = true;
                int j = 0;
                while (j < tiles.Length && tiles[j].colour != i) j++;
                if (j < tiles.Length)
                {
                    tiles[j].CheckGroup(i);
                    foreach (BoardTile t in tiles) grouped[i] &= t.colour != i || t.GetChecked();
                    foreach (BoardTile t in tiles) t.Uncheck();
                    completed &= grouped[i];
                }
                for (int k = 0; k < tiles.Length; k++) if (tiles[k].colour == i) slots[k].SetGrouped(grouped[i]);
            }
        }
        else // blink
        {
            int c = tiles[0].colour;
            foreach (BoardTile t in tiles)
            {
                if (t.colour != c)
                {
                    completed = false;
                    break;
                }
            }
            foreach (BoardSlot s in slots) s.SetGrouped(completed);
        }
        if (completed) foreach (BoardTile t in tiles) t.SetSelected(false);
    }

    /// <summary>
    /// Return a list of all tiles adjacent to the passed tile.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <returns>All tiles on the board which are exactly one space away from the passed tile.</returns>
    public List<BoardTile> GetAdjacentTiles(BoardTile tile)
    {
        List<BoardTile> adjacent = new List<BoardTile>();
        for (int i = 0; i < tiles.Length; i++) if (Mathf.Abs(tile.x - tiles[i].x) + Mathf.Abs(tile.y - tiles[i].y) + Mathf.Abs(tile.z - tiles[i].z) == 2) adjacent.Add(tiles[i]);
        return adjacent;
    }

    /// <summary>
    /// Return the position of a tile with the passed coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    /// <returns>The calculated position.</returns>
    public Vector2 GetPosition(int x, int y, int z)
    {
        return new Vector2(transform.position.x, transform.position.y) + new Vector2(x + z, -y * Mathf.Sqrt(3)) * boardScale / (n * 2 + 1);
    }

    /// <summary>
    /// Set the active state of the board to the passed boolean value.
    /// </summary>
    /// <param name="a">The new active state.</param>
    public void SetActive(bool a)
    {
        active = a;
    }

    /// <summary>
    /// Return the number of moves that have been made.
    /// </summary>
    /// <returns>The number of moves.</returns>
    public int GetMoveCount()
    {
        return moveCount;
    }

    /// <summary>
    /// Return the completion status of the board.
    /// </summary>
    /// <returns>Whether the board is solved.</returns>
    public bool GetCompleted()
    {
        return completed;
    }

    /// <summary>
    /// Return the index of the pack this board belongs to.
    /// </summary>
    /// <returns>The pack index.</returns>
    public int GetPack()
    {
        return pack;
    }

    /// <summary>
    /// Check the alignment of the two tiles and return a char value based on the result.
    /// </summary>
    /// <param name="index1">The index of the first tile.</param>
    /// <param name="index2">The index of the second tile.</param>
    /// <returns>A char representing the alignment value of the two tiles.</returns>
    private char Alignment(int index1, int index2)
    {
        if (index1 < 0 || index2 < 0) return ' ';
        return tiles[index1].x == tiles[index2].x ? 'x' : tiles[index1].y == tiles[index2].y ? 'y' : tiles[index1].z == tiles[index2].z ? 'z' : ' ';
    }

    /// <summary>
    /// Return true if the third tile is situated between the other two tiles in a line, false otherwise.
    /// </summary>
    /// <param name="index1">The index of the first tile.</param>
    /// <param name="index2">The index of the second tile.</param>
    /// <param name="index3">The index of the third tile.</param>
    /// <returns>The result of the evaluation.</returns>
    private bool Between(int index1, int index2, int index3)
    {
        if (Alignment(index1, index2) == ' ' || Alignment(index1, index2) != Alignment(index1, index3)) return false;
        return (tiles[index1].x < tiles[index2].x ? (tiles[index3].x >= tiles[index1].x && tiles[index3].x <= tiles[index2].x) : (tiles[index3].x <= tiles[index1].x && tiles[index3].x >= tiles[index2].x)) && (tiles[index1].y < tiles[index2].y ? (tiles[index3].y >= tiles[index1].y && tiles[index3].y <= tiles[index2].y) : (tiles[index3].y <= tiles[index1].y && tiles[index3].y >= tiles[index2].y)) && (tiles[index1].z < tiles[index2].z ? (tiles[index3].z >= tiles[index1].z && tiles[index3].z <= tiles[index2].z) : (tiles[index3].z <= tiles[index1].z && tiles[index3].z >= tiles[index2].z));
    }

    /// <summary>
    /// Return true if the three tiles form the corners of a triangle, false otherwise.
    /// </summary>
    /// <param name="index1">The index of the first tile.</param>
    /// <param name="index2">The index of the second tile.</param>
    /// <param name="index3">The index of the third tile.</param>
    /// <returns>The result of the evaluation.</returns>
    private bool FormTriangle(int index1, int index2, int index3)
    {
        if (index1 < 0 || index2 < 0 || index3 < 0) return false;
        if (index1 == index2 && index1 == index3) return true;
        char alignment1 = Alignment(index1, index2);
        char alignment2 = Alignment(index1, index3);
        char alignment3 = Alignment(index2, index3);
        return alignment1 != ' ' && alignment1 != alignment2 && alignment1 != alignment3 && alignment2 != ' ' && alignment2 != alignment3 && alignment3 != ' ';
    }

    /// <summary>
    /// Returns true if the fourth tile is contained in the triangle formed by the first three tiles, false otherwise.
    /// </summary>
    /// <param name="index1">The index of the first tile.</param>
    /// <param name="index2">The index of the second tile.</param>
    /// <param name="index3">The index of the third tile.</param>
    /// <param name="index4">The index of the fourth tile.</param>
    /// <param name="edgeOnly">Whether the fourth tile must be aligned with one of the other tiles.</param>
    /// <returns>The result of the evaluation.</returns>
    private bool InTriangle(int index1, int index2, int index3, int index4, bool edgeOnly)
    {
        if (!FormTriangle(index1, index2, index3) || index4 < 0) return false;
        if (index1 == index2 && index1 == index3) return index1 == index4;
        if (index1 == index4 || index2 == index4 || index3 == index4) return true;
        if (Between(index1, index2, index4) || Between(index1, index3, index4) || Between(index2, index3, index4)) return true;
        if (edgeOnly) return false;
        char alignment1 = Alignment(index1, index2);
        char alignment2 = Alignment(index1, index3);
        char alignment3 = Alignment(index2, index3);
        bool b = true;
        int[] point1 = new int[] { alignment1 == 'x' ? tiles[index1].x : alignment1 == 'y' ? tiles[index1].y : tiles[index1].z, alignment2 == 'x' ? tiles[index1].x : alignment2 == 'y' ? tiles[index1].y : tiles[index1].z, alignment3 == 'x' ? tiles[index1].x : alignment3 == 'y' ? tiles[index1].y : tiles[index1].z };
        int[] point2 = new int[] { alignment1 == 'x' ? tiles[index3].x : alignment1 == 'y' ? tiles[index3].y : tiles[index3].z, alignment2 == 'x' ? tiles[index2].x : alignment2 == 'y' ? tiles[index2].y : tiles[index2].z, alignment3 == 'x' ? tiles[index2].x : alignment3 == 'y' ? tiles[index2].y : tiles[index2].z };
        int[] point3 = new int[] { alignment1 == 'x' ? tiles[index4].x : alignment1 == 'y' ? tiles[index4].y : tiles[index4].z, alignment2 == 'x' ? tiles[index4].x : alignment2 == 'y' ? tiles[index4].y : tiles[index4].z, alignment3 == 'x' ? tiles[index4].x : alignment3 == 'y' ? tiles[index4].y : tiles[index4].z };
        for (int i = 0; i < 3; i++) b &= (point1[i] < point2[i] ? point1[i] <= point3[i] && point2[i] >= point3[i] : point1[i] >= point3[i] && point2[i] <= point3[i]);
        return b;
    }
}
