using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    public Vector2 size;
    private bool[,] heights;
    private bool[,] previousFloor;
    private List<Vector2> points;
    public GameObject tiles;
    public Transform wallL;
    public Transform wallR;
    public Transform wallB;
    public GameObject spawner;
    public GameManager manager;
    private int startZ;
    private int startX;

    /// HARDCAP AVERAGES ARE 0.354% OF THE MAP SCALE. IF PLAYTESTING REVEALS VERY UNSATISFACTORY GENERATION, A LIMIT AND REGEN FUNCTION WILL BE IMPLEMENTED

    void Start()
    {
        //Generate map layout, and then decorate with loot
        Generate();
        Interiors();
        PlaceMap();
    }

    //Expands off of the previous map layout when size changes
    public void CopyMap(int increase)
    {
        //Ensure only even numbers are passed
        increase += increase % 2;

        //Reduce map just generates a new layout
        size = new Vector2(Mathf.Clamp(size.x + increase, 12, 252), Mathf.Clamp(size.y + increase, 12, 252));
        if (increase < 0) {
            Generate();
            Interiors();
            PlaceMap();
            return;
        }

        //Copies over the previous layout onto the new larger layout
        previousFloor = heights;
        heights = new bool[(int)size.x + 1, (int)size.y + 1];
        for (int i = increase / 2; i < size.x - (increase / 2 - 1); i++) {
            for (int j = 0; j < size.y - (increase - 1); j++) {
                heights[i, j] = previousFloor[i - increase / 2, j];
            }
        }

        //Mark dead ends and then expand from them, but only if they align with the odd grid
        points.Clear();
        bool[,] ends = DeadEnds();
        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                if (i % 2 == 0 && j % 2 == 1 && ends[i, j]) Check(i, j);
            }
        }

        //More maze
        StartPoints();
        Mazercise(4, 0, -1);
        Diagonals(0);
        Interiors();
        PlaceMap();
    }

    //Destroys all objects from map for reset
    public void ClearMap()
    {
        for (int c = transform.childCount - 1; c >= 0; c--) Destroy(transform.GetChild(c).gameObject);
    }

    //Place tiles
    private void PlaceMap()
    {
        //Move walls
        wallB.position = new Vector3(0, 1.5f, size.y + 1);
        wallL.position = new Vector3((size.x + 1) / 2, 1.5f, 127.5f);
        wallR.position = new Vector3((size.x + 1) / -2, 1.5f, 127.5f);

        for (int i = 0; i <= size.x; i++) {
            for (int j = 0; j <= size.y; j++) {
                if (heights[i, j]) Instantiate(tiles, new Vector3(i - size.x / 2, 0, j), Quaternion.identity, transform);
            }
        }
    }

    //Start map
    private void Generate()
    {
        //Start with all points as walls
        heights = new bool[(int)size.x + 1, (int)size.y + 1];

        StartPoints();

        //Record valid points in cardinal directions from start
        points = new List<Vector2>();
        Check(startX, startZ);

        Mazercise(1, 0, 3);
    }

    //Starting point at entrance
    private void StartPoints()
    {
        startX = (int)(size.x / 2);
        startZ = 1;
        heights[startX, startZ] = true;
    }

    //Expand maze algorithm
    private void Mazercise(int Prune1, int Expansion, int Prune2)
    {
        //Until all valid points have been used, randomly pick, check, and expand it, then remove from list
        while (points.Count > 0) {
            int index = Random.Range(0, points.Count);
            int x = (int)points[index].x;
            int y = (int)points[index].y;
            heights[x, y] = true;
            points.RemoveAt(index);

            //Randomly check cardinal points until one that isnt a wall is found
            List<int> cardinals = new List<int>() { 0, 1, 2, 3 };
            while (cardinals.Count > 0) {
                int i = Random.Range(0, cardinals.Count);
                switch (cardinals[i]) {
                    case 0:
                        if (y - 2 >= 0 && heights[x, y - 2] && !heights[x, y - 1]) {
                            heights[x, y - 1] = true;
                            cardinals.Clear();
                        }
                        else cardinals.RemoveAt(i);
                        break;
                    case 1:
                        if (y + 2 < size.y && heights[x, y + 2] && !heights[x, y + 1]) {
                            heights[x, y + 1] = true;
                            cardinals.Clear();
                        }
                        else cardinals.RemoveAt(i);
                        break;
                    case 2:
                        if (x - 2 >= 0 && heights[x - 2, y] && !heights[x - 1, y]) {
                            heights[x - 1, y] = true;
                            cardinals.Clear();
                        }
                        else cardinals.RemoveAt(i);
                        break;
                    case 3:
                        if (x + 2 < size.x && heights[x + 2, y] && !heights[x + 1, y]) {
                            heights[x + 1, y] = true;
                            cardinals.Clear();
                        }
                        else cardinals.RemoveAt(i);
                        break;
                }
            }

            //Add valid cells and repeat
            Check(x, y);
        }

        //Expand dead ends to connect close sections, then prune all other dead ends
        Prune(Prune1);
        Expand(Expansion);
        Prune(Prune2);

        //Clean up one end dead ends and border
        Diagonals(0);
        for (int x = 0; x <= size.x; x++) {
            heights[0, x] = false;
            heights[(int)size.x, x] = false;
        }
        for (int z = 0; z <= size.y; z++) {
            heights[z, 0] = false;
            heights[z, (int)size.y] = false;
        }

        //Replace the start area to ensure the path is there
        for (int i = -1; i < 2; i++) {
            for (int j = -1; j < 2; j++) {
                heights[startX + i, startZ + j] = true;
            }
        }
    }

    //Prune dead ends by given amount
    private void Prune(int amount)
    {
        //Check if more than 1 neighbor, otherwise delete (except start node)
        bool[,] remove = DeadEnds();

        //Loop through listed dead ends and remove them to prevent cascading
        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
                if (remove[i, j] == true) heights[i, j] = false;
            }
        }

        //Recursion
        if (amount > 0) Prune(amount - 1);
    }

    //Expand dead ends outward to nearest node
    private void Expand(int amount)
    {
        //Check if only 1 neighbor not connected, fill in the gap
        bool[,] add = DeadEnds();

        //Loop through listed dead ends and expand them
        for (int i = 1; i < size.x; i++) {
            for (int j = 1; j < size.y; j++) {
                if (add[i, j] == true) {
                    int z = Random.Range(0, 4);
                    switch (z) {
                        case 0:
                            if (!heights[i + 1, j]) heights[i + 1, j] = true;
                            continue;
                        case 1:
                            if (!heights[i - 1, j]) heights[i - 1, j] = true;
                            continue;
                        case 2:
                            if (!heights[i, j + 1]) heights[i, j + 1] = true;
                            continue;
                        default:
                            if (!heights[i, j - 1]) heights[i, j - 1] = true;
                            continue;
                    }
                }
            }
        }

        //Recursion
        if (amount > 0) Expand(amount - 1);
    }

    //Mark interior sections for loot
    public void Interiors()
    {
        //Place spawner locations
        List<Transform> spawns = new List<Transform>();
        for (int i = 1; i < size.x; i++) {
            for (int j = 1; j < size.y; j++) {
                if (Neighbors(i, j) < 1) {
                    GameObject t = Instantiate(spawner, new Vector3(i + 1 - size.x / 2, 0, j), Quaternion.identity, transform) as GameObject;
                    spawns.Add(t.transform);
                }
            }
        }

        //Place loot amount based on manager
        for (int i = 0; i < manager.lootAmount; i++) {
            int r = Random.Range(0, manager.loot.Length);
            int s = Random.Range(0, spawns.Count);
            Instantiate(manager.loot[r], spawns[s].position, Quaternion.identity);
            spawns.RemoveAt(s);
        }

        for (int i = 0; i < spawns.Count; i++) Destroy(spawns[i].gameObject);
        spawns.Clear();
    }

    //Find a list of dead ends
    private bool[,] DeadEnds()
    {
        bool[,] ends = new bool[(int)size.x, (int)size.y];
        for (int x = 1; x < size.x; x++) {
            for (int y = 1; y < size.y; y++) {
                if (heights[x, y]) {
                    if (x == startX || y == startZ) continue;
                    ends[x, y] = (Neighbors(x, y) <= 1);
                }
            }
        }
        return ends;
    }

    //Check diagonals for solo dead ends
    private void Diagonals(int amount)
    {
        for (int x = 1; x < size.x; x++) {
            for (int y = 1; y < size.y; y++) {
                if (heights[x, y]) {
                    if (x == startX || y == startZ) continue;
                    int diagonals = 0;
                    if (heights[x + 1, y + 1]) diagonals++;
                    if (heights[x + 1, y - 1]) diagonals++;
                    if (heights[x - 1, y + 1]) diagonals++;
                    if (heights[x - 1, y - 1]) diagonals++;
                    if (diagonals > 1 && Neighbors(x, y) <= 1) heights[x, y] = false;
                }
            }
        }

        //Recursion
        if (amount > 0) Diagonals(amount - 1);
    }

    //Adjacents
    private int Neighbors(int x, int y)
    {
        int adjacent = 0;
        if (heights[x, y - 1]) adjacent++;
        if (heights[x, y + 1]) adjacent++;
        if (heights[x - 1, y]) adjacent++;
        if (heights[x + 1, y]) adjacent++;
        return adjacent;
    }

    //Check if 2 spaces away is viable
    void Check(int x, int y)
    {
        if (y - 2 >= 0 && !heights[x, y - 2]) CreatePoint(new Vector2(x, y - 2));
        if (y + 2 < size.y && !heights[x, y + 2]) CreatePoint(new Vector2(x, y + 2));
        if (x - 2 >= 0 && !heights[x - 2, y]) CreatePoint(new Vector2(x - 2, y));
        if (x + 2 < size.x && !heights[x + 2, y]) CreatePoint(new Vector2(x + 2, y));
    }

    //Add point to list if not already on there
    void CreatePoint(Vector2 point)
    {
        if (!points.Contains(point)) points.Add(point);
    }
}