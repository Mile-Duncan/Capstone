using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;

public class Tile
{
    private const float GeneralVerticalNoiseMultiplyer = 2f;
    private const float GeneralNoiseStreach = .2f;
    private const float GeneralContinentialnessMultiplyer = 30f;
    private const float GeneralContinentialHeightMultiplyer = 5f;
    private const float GeneralMountainRarity = 4f;
    private const float GeneralMountainJagadness = 2.6f;
    private const float GeneralMountainSpeapness = 5f;
    private const float MountainVerticalNoiseMultiplyer = 10f;
    private const float EdgeSmothing = 30;

    public int DetailSize;
    public int MeshSize;
    private int Detail;
    private Mesh mesh;

    public GameObject NewTile(int i, int j, int ms, int detail)
    {
        DetailSize = ms/detail;
        Detail = detail;
        MeshSize = ms;
        
        CreateMesh(i,j);
        
        GameObject gameObject = new GameObject("Tile", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(LODGroup));
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.transform.Translate(i, 0, j);
        gameObject.GetComponent<MeshCollider>().enabled = true;
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        
        
        GameObject ConnectedObjects = new GameObject("Connected Objects", typeof(ObjectData));
        GameObject Genorated = new GameObject("Genorated", typeof(ObjectData));

        ConnectedObjects.transform.position = gameObject.transform.position;
        Genorated.transform.position = gameObject.transform.position;
        
        ConnectedObjects.GetComponent<ObjectData>().setInteractable(true);
        Genorated.GetComponent<ObjectData>().setInteractable(true);
        
        ConnectedObjects.transform.parent = gameObject.transform;
        Genorated.transform.parent = gameObject.transform;
        

        TileDecorator decorator = new TileDecorator(gameObject, MeshSize, Detail);

        gameObject = decorator.Decorate();
        

        gameObject.transform.SetParent(TileManager.TileGrid.transform);
        
        return gameObject;
    }

    private void CreateMesh(int i, int j)
    {
        mesh = new Mesh();
        mesh.Clear();

        Vector3[] vertices = new Vector3[(DetailSize+1)*(DetailSize+1)];
        for (int x = 0, w = 0; x <= DetailSize*Detail; x+=Detail)for(int z = 0; z <= DetailSize*Detail; z+=Detail)
        {

            double perlinXValue = (x + i) * GeneralNoiseStreach + TileManager.PerlinOffsetX;
            double perlinYValue = (z + j) * GeneralNoiseStreach + TileManager.PerlinOffsetZ;
            
            
            vertices[w] = new Vector3(x,(float)CalculateNoise((x + i),(z + j),perlinXValue, perlinYValue),z);
            w++;
        }
        
        mesh.vertices = vertices;

        int[] triangles = new int[3];
        
        int vert = 0;
        int tris = 0;
        mesh.subMeshCount = DetailSize*DetailSize*2;
        for (int z = 0; z < DetailSize; z++)
        {
            for (int x = 0; x < DetailSize; x++)
            {
                if ((z + x) % 2 == 0)
                {
                    triangles[2] = vert + 0;
                    triangles[1] = vert + DetailSize + 1;
                    triangles[0] = vert + 1;
                    mesh.SetTriangles(triangles, tris);
                    tris++;
                    triangles[2] = vert + 1;
                    triangles[1] = vert + DetailSize + 1;
                    triangles[0] = vert + DetailSize + 2;
                    mesh.SetTriangles(triangles, tris);
                }
                else
                {
                    triangles[2] = vert + 0;
                    triangles[1] = vert + DetailSize + 1;
                    triangles[0] = vert + DetailSize + 2;
                    mesh.SetTriangles(triangles, tris);
                    tris++;
                    triangles[0] = vert + 0;
                    triangles[1] = vert + 1;
                    triangles[2] = vert + DetailSize + 2;
                    mesh.SetTriangles(triangles, tris);
                }

                vert++;
                tris++;
            }
            vert++;
        }
    }

    private double CalculateNoise(int trueX, int trueY, double perlinXValue, double perlinYValue)
    {
        float GenericVeriticalNoise = CalculateBumpyness(perlinXValue, perlinYValue)/2;

        float GenericTerrainHeight = CalculateTerrainHeight(perlinXValue, perlinYValue);

        return SoftenEdge(new Vector2(trueX, trueY),GenericTerrainHeight + GenericVeriticalNoise);

    }
    
    private static double SoftenEdge(Vector2 tilePos, double noise)
    {
        float dist = Vector2.Distance(tilePos, new Vector2(TileManager.MapSize / 2f, TileManager.MapSize / 2f));
        float amount = ((TileManager.MapSize / 2f) - EdgeSmothing*5);
        if (dist > amount)
        {
            float subAmount = (Mathf.Pow((amount-dist)/EdgeSmothing,2));
            //if(subAmount < -EdgeSmothing) subAmount = -EdgeSmothing;
            noise -= subAmount;
            if(noise < -10) noise = -10;

            //noise = 0;
        }
        return noise;
    }

    private float CalculateBumpyness(double perlinXValue, double perlinYValue)
    {
        return Mathf.PerlinNoise((float)perlinXValue, (float)perlinYValue) * GeneralVerticalNoiseMultiplyer;
    }
    private float CalculateTerrainHeight(double perlinXValue, double perlinYValue)
    {
        float GenericTerrainHeight = Mathf.PerlinNoise((float)(perlinXValue/GeneralContinentialnessMultiplyer), (float)(perlinYValue/GeneralContinentialnessMultiplyer)) * GeneralVerticalNoiseMultiplyer;

        GenericTerrainHeight = GenericTerrainHeight * 2 - 1.5f;
        
        GenericTerrainHeight = (float)Math.Pow(Math.Abs(GenericTerrainHeight),1.0/1.5)*Math.Abs(GenericTerrainHeight)/GenericTerrainHeight;
        
        GenericTerrainHeight *= GeneralContinentialHeightMultiplyer;

        GenericTerrainHeight = CalculateMountains(perlinXValue, perlinYValue, GenericTerrainHeight);
        return GenericTerrainHeight;
    }

    private float CalculateMountains(double perlinXValue, double perlinYValue, float GenericTerrainHeight)
    {
        float MountainNoise = Mathf.PerlinNoise((float)perlinXValue/(10-GeneralMountainJagadness), (float)perlinYValue/(10-GeneralMountainJagadness));
        MountainNoise = GeneralVerticalNoiseMultiplyer * MountainNoise - 1;        

        float MountainPeak = (float)Math.Pow(Math.Sqrt(Math.Abs(GeneralMountainRarity-Math.Abs(GenericTerrainHeight-(GeneralMountainJagadness*MountainNoise)))), GeneralMountainSpeapness);
        if (!(MountainPeak > 0)) MountainPeak = 0;

        if (MountainNoise + (GenericTerrainHeight * GeneralVerticalNoiseMultiplyer) >=
            GeneralVerticalNoiseMultiplyer * GeneralMountainRarity)
        {
            GenericTerrainHeight += MountainPeak + (MountainNoise * GeneralMountainJagadness) - MountainNoise;
            GenericTerrainHeight += (CalculateBumpyness(perlinXValue, perlinYValue)*GenericTerrainHeight*GeneralMountainJagadness)/100*MountainVerticalNoiseMultiplyer-CalculateBumpyness(perlinXValue, perlinYValue);
        }
        return GenericTerrainHeight;
    }
}
