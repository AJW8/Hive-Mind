using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStack : MonoBehaviour
{
    // each move is represented by a pair of indices representing those of the two tiles the move involves (in the case of the blink pack whose moves only involves one tile, the two indices are the same for each move)
    private string stack;

    void Awake()
    {
        stack = "";
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Push the passed move onto the stack.
    /// </summary>
    /// <param name="index1">The index of the first tile in the move.</param>
    /// <param name="index2">The index of the second tile in the move.</param>
    /// <param name="index3">The index of the third tile in the move.</param>
    public void Push(int index1, int index2, int index3) // 'push' new move onto stack
    {
        if (!Empty()) stack += ",";
        stack += index1 + ":" + index2 + ":" + index3;
    }

    /// <summary>
    /// Pop and return the top move on the stack.
    /// </summary>
    /// <returns></returns>
    public int[] Pop() // 'pop' most recently added move off stack
    {
        if (Empty()) return new int[] { -1, -1 };
        string[] split = stack.Split(',');
        stack = "";
        for (int i = 0; i < split.Length - 1; i++)
        {
            if (i > 0) stack += ",";
            stack += split[i];
        }
        split = split[split.Length - 1].Split(':');
        return new int[] { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]) };
    }

    /// <summary>
    /// Return true if the stack is currently empty, false otherwise.
    /// </summary>
    /// <returns>Whether the stack is currently empty.</returns>
    public bool Empty()
    {
        return stack.Length == 0;
    }
}
