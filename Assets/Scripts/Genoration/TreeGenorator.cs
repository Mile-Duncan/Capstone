
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TreeGenorator
{
    private static int TreeSeed;
    private static float XPos;
    private static float ZPos;
    private static int LastRandom;

    public static GameObject Test(float X, float Y, float Z, GameObject Tile)
    {
        LastRandom = (int)(X + Y - Z);
        XPos = Mathf.Abs(X);
        ZPos = Mathf.Abs(Z);
        TreeSeed = (int)(X + Z - TileManager.Seed);
        GameObject Tree = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        Tree.transform.rotation = new Quaternion(0 ,GetRandom()%360 ,0, 0);
        Tree.transform.position = new Vector3(X + (GetRandom()%10)/5-0.5f, Y-1f, Z+(GetRandom()%10)/5-0.5f);
        
        Tree.transform.SetParent(Tile.transform);
        
        return Tree;
    }

    public static GameObject OakTree(Vector3 Pos, GameObject Tile)
    {
        return OakTree(Pos[0], Pos[1], Pos[2], Tile);
    }
    public static GameObject OakTree(float X, float Y, float Z, GameObject Tile)
    {
        LastRandom = (int)(X + Y - Z);
        XPos = Mathf.Abs(X);
        ZPos = Mathf.Abs(Z);
        TreeSeed = (int)(X + Z - TileManager.Seed);
        GameObject Tree = new GameObject("Oak Tree");
        
        Tree.AddComponent<ObjectData>();
        
        ObjectData TreeData = setData(Tree.GetComponent<ObjectData>(), new Vector3(X,Y,Z));


        //GameObject CheckTile = TileManager.Save.GetTileAt(TreeData.getOriginalXZPosition());
        Vector2 tilePos = new Vector2(Tile.transform.position.x, Tile.transform.position.z);
        
        if (TileManager.Save.SavedTileObjectKeys.Contains(tilePos))
        {
            List<ObjectData> TileObjects = TileManager.Save.SavedTileObjects[tilePos];
            if (TileObjects.Count != 0)
            {
                for (int i = 0; i < TileObjects.Count; i++)
                {
                    if (TileObjects[i].getOriginalPosition() == new Vector3(X,Y,Z) && TileObjects[i].getType() == "Hidden")
                    {
                        MonoBehaviour.DestroyImmediate(Tree);
                        return null;
                    }
                }
            }
        }

        GameObject Trunk = new GameObject("TreeTrunk", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        Trunk.GetComponent<MeshFilter>().mesh = GenaricTrunk();
        Trunk.GetComponent<MeshCollider>().enabled = true;
        Trunk.GetComponent<MeshCollider>().sharedMesh = Trunk.GetComponent<MeshFilter>().mesh;
        Trunk.transform.SetParent(Tree.transform);
        Trunk.GetComponent<Renderer>().material = Resorces.Materials["Log"];

        for (int i = GetRandom()%3; i < 5; i++)
        {
            Vector3 TreeTop = Trunk.GetComponent<MeshFilter>().mesh.vertices[3];
                
            GameObject Leaf = new GameObject("TreeLeaf", typeof(MeshFilter), typeof(MeshRenderer));
            Leaf.transform.SetParent(Tree.transform);

            Leaf.GetComponent<MeshFilter>().mesh = GenaricLeafs();
            Leaf.transform.rotation = new Quaternion(GetRandom()%360 ,GetRandom()%360 ,GetRandom()%360, 0);
            float size = 2.5f;
            Leaf.transform.localScale += new Vector3(GetRandom()%10/9+size, GetRandom()%10/9+size, GetRandom()%10/9+size);

            if(Leaf.transform.localScale[2]<0)MonoBehaviour.print("Error");
            Vector3 Offset = Leaf.transform.position - Leaf.transform.TransformPoint(Leaf.GetComponent<MeshFilter>().mesh.bounds.center);

            Leaf.transform.position = Offset + TreeTop - new Vector3(0,GetRandom()%10/8+2,0);
            Leaf.GetComponent<Renderer>().material = Resorces.Materials["Leaf"];
        }

        Tree.transform.rotation = new Quaternion(0 ,GetRandom()%360 ,0, 0);
        Tree.transform.position = new Vector3(X + (GetRandom()%10)/5-0.5f, Y-1f, Z+(GetRandom()%10)/5-0.5f);

        for (int i = 0; i < Tile.transform.childCount; i++)
        {
            if(Tile.transform.GetChild(i).gameObject.name=="Genorated")
            {
                Tree.transform.SetParent(Tile.transform.GetChild(i));
                break;
            }
        }
        Tree.gameObject.tag = "Genorated";

        
        return Tree;
    }

    private static Mesh GenaricTrunk()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[12];

        float Thickness = 0.5f;

        vertices[0] = new Vector3(0 + Thickness/((1 * XPos)%5+1f), 0, 0 + Thickness/((1 * ZPos)%5+1f));
        vertices[1] = new Vector3(0 + Thickness/((2 * XPos)%5+1f), 0, 1 + Thickness/((2 * ZPos)%5+1f));
        vertices[2] = new Vector3(0.6f + Thickness/((3 * XPos)%5+1f), 0, 0.6f + Thickness/((3 * ZPos)%5+1f));
        
        vertices[3] = new Vector3(0.3f + Thickness/((5 * XPos)%5+1f), 8, 0.3f + Thickness/((5 * ZPos)%5+1f));

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;
        triangles[6] = 2;
        triangles[7] = 0;
        triangles[8] = 3;
        triangles[9] = 0;
        triangles[10] = 2;
        triangles[11] = 1;
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();

        return mesh;
    }
    
    private static Mesh GenaricLeafs()
    {
        int offset = 16;
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[12];
        int[] triangles = new int[12];

        float Thickness = 0.3f;
        
        vertices[0] = new Vector3(0 + Thickness/(1 * (offset+XPos)%5+1f), 0, 0 + Thickness/(1 * (offset+ZPos)%5+1f));
        vertices[1] = new Vector3(0 + Thickness/(2 * (offset+XPos)%5+1f), 0, 1 + Thickness/(2 * (offset+ZPos)%5+1f));
        vertices[2] = new Vector3(1.2f + Thickness/(3 * (offset+XPos)%5+1f), 0, 1.2f + Thickness/(3 * (offset+ZPos)%5+1f));
        vertices[3] = new Vector3(0.3f + Thickness/(5 * (offset+XPos)%5+1f), 0.7f, 0.3f + Thickness/(5 * (offset+ZPos)%5+1f));
        
        
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;
        triangles[6] = 2;
        triangles[7] = 0;
        triangles[8] = 3;
        triangles[9] = 0;
        triangles[10] = 2;
        triangles[11] = 1;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();

        return mesh;
    }

    private static ObjectData setData(ObjectData TreeData, Vector3 pos)
    {
        TreeData.setType("Tree");
        TreeData.setOriginalPosition(pos);
        TreeData.setItemDrop("Log");
        TreeData.setBreakAnimation("Tree");
        TreeData.setGenorated(true);
        TreeData.setInteractable(true);
        return TreeData;
    }

    private static int GetRandom()
    {
        LastRandom = TileManager.GetRandom(LastRandom);
        return LastRandom;
    }
}
