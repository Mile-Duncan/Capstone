
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class TileDecorator
{
    private const float GeneralBiomeSizeMultiplyer = 800f;

    private float XPos;    
    private float YLevel;
    private float ZPos;
    private float SubX;
    private float SubY;
    private float SubZ;
    private Mesh Mesh;
    private int DetailSize;
    private int MeshSize;
    private int SubDetailMeshSize;
    private int SubMeshSize;
    private SubMeshDescriptor SubMesh;
    private GameObject CurrentTile;
    private string[] SubTypes;
    private string Type;
    
    public TileDecorator(GameObject currentTile, int meshSize, int detail)
    {
        CurrentTile = currentTile;
        DetailSize = meshSize/detail;
        MeshSize = meshSize;
        
        
        SubDetailMeshSize = DetailSize * DetailSize * 2;

        SubMeshSize = MeshSize * MeshSize * 2;
        
        Mesh = CurrentTile.GetComponent<MeshFilter>().mesh;
        
        XPos = CurrentTile.transform.position[0];
        YLevel = Mesh.vertices[DetailSize][1];
        ZPos = CurrentTile.transform.position[2];

    }

    public GameObject Decorate()
    {
        SubTypes = new string[SubDetailMeshSize];
        for (int i = 0; i < SubDetailMeshSize; i++)
        {
            SubMesh = Mesh.GetSubMesh(i);
            
            SubX = SubMesh.bounds.center[0]+XPos;
            SubY = SubMesh.bounds.center[1];
            SubZ = SubMesh.bounds.center[2]+ZPos;
            
            SetType(i);
            
            if(i%2==0)PlaceTrees(i);
        } 
        
        SetMaterials();
        
        Type = SubTypes[(SubDetailMeshSize) / 2];

        CombineSimlerSubMesh();

        Mesh.RecalculateNormals();
        CurrentTile.GetComponent<MeshFilter>().mesh = Mesh;
        return CurrentTile;
    }
    
    private void SetType(int i)
    {

        if(SubY>50)SubTypes[i] = "Snow";
        else if(SubY>15)SubTypes[i] = "Stone";
        else if (SubY >= 0.4)
        {
            float Biome = Mathf.PerlinNoise((SubX/GeneralBiomeSizeMultiplyer - TileManager.Seed), (SubZ/GeneralBiomeSizeMultiplyer - TileManager.Seed));
            if( Biome > 0.65)SubTypes[i] = "Sand";
            else if (Biome > 0.47) SubTypes[i] = "Grass";
            else SubTypes[i] = "Forest";
        }
        else SubTypes[i] = "Dirt";
    }
    
    private void SetMaterials()
    {
        Material[] materials = new Material[SubDetailMeshSize];
        Renderer rend = CurrentTile.GetComponent<Renderer>();
        for(int i=0; i<SubTypes.Length; i++)materials[i] = Resorces.Materials[SubTypes[i]];
        rend.materials = materials;
    }

    private void CombineSimlerSubMesh()
    {
        MeshRenderer rend = CurrentTile.GetComponent<MeshRenderer>();

        Mesh NewMesh = new Mesh();
        NewMesh.subMeshCount=0;
        NewMesh.vertices = Mesh.vertices;

        List<string> materialNames = new List<string>();
        List<Material> materialsInMesh = new List<Material>();

        
        for (int k = 0; k < rend.materials.Length; k++) if (!materialNames.Contains(rend.materials[k].name))
        {
            materialNames.Add(rend.materials[k].name);
            NewMesh.subMeshCount=materialNames.Count;

            int[] SubTries = CreateLargerSubmesh(rend, rend.materials[k]).ToArray();
            NewMesh.SetTriangles(SubTries,materialNames.Count-1);
            materialsInMesh.Add(rend.materials[k]);
        }

        if (NewMesh.subMeshCount != materialsInMesh.Count) return;
        
        rend.materials = materialsInMesh.ToArray();

        Mesh = NewMesh;
    }

    private List<int> CreateLargerSubmesh(MeshRenderer rend, Material SubMaterial)
    {
        List<int> tris = new List<int>();
        for (int i = 0, h = 0; i < Mesh.subMeshCount; i++)
        {
            if(rend.materials[i].name==SubMaterial.name) for (int j = 0; j < Mesh.GetTriangles(i).Length; j++)
            {
                tris.Add(Mesh.GetTriangles(i)[j]);
                h++;
            }
        }

        return tris;
    }

    private void PlaceTrees(int i)
    {
        int rand = TileManager.GetRandom((int)((SubX * SubZ) + SubY * (SubZ - SubX)), 0, 1000);
        
        if (SubTypes[i] == "Grass" && rand > 998) TreeGenorator.OakTree(SubX, SubY, SubZ, CurrentTile);
        if (SubTypes[i] == "Forest" && rand > 960) TreeGenorator.OakTree(SubX, SubY, SubZ, CurrentTile);
        
        //if (SubTypes[i] == "Grass" && rand > 997) TreeGenorator.Test(SubX, SubY, SubZ, CurrentTile);
        //if (SubTypes[i] == "Forest" && rand > 950) TreeGenorator.Test(SubX, SubY, SubZ, CurrentTile);

    }
    
}
