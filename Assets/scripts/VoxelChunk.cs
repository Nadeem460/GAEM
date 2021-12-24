using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Faces
{
    Up,
    Down,
    Right,
    Left,
    Front,
    Back
}

public class VoxelChunk : MonoBehaviour
{
    public Land land;
    public ItemPrefabs itemPrefabs;
    public BlockToItemID blockToItemID;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public short[,,] blockIDs;
    public bool requiresMeshGeneration = false;
    public int vertexLength;
    public Vector2Int resolution;

    public void WakeUp()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        blockIDs = new short[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    blockIDs[x, y, z] = 0;
                }
            }
        }

        requiresMeshGeneration = true;
    }

    private void Update()
    {
        if (requiresMeshGeneration)
        {
            GenerateMesh();
        }
    }

    void GenerateMesh()
    {
        Mesh normalMesh = new Mesh();
        List<MeshFilter> customMeshFilters = new List<MeshFilter>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int currentIndex = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    if (blockIDs[x, y, z] == 0) continue;
                    else
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        GenerateBlock_Top(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Up), pos, customMeshFilters);
                        GenerateBlock_Right(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Right), pos, customMeshFilters);
                        GenerateBlock_Left(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Left), pos, customMeshFilters);
                        GenerateBlock_Forward(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Front), pos, customMeshFilters);
                        GenerateBlock_Back(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Back), pos, customMeshFilters);
                        GenerateBlock_Bottom(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Down), pos, customMeshFilters);
                    }
                }
            }
        }

        normalMesh.SetVertices(vertices);
        normalMesh.SetNormals(normals);
        normalMesh.SetUVs(0, uvs);
        normalMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        normalMesh.RecalculateTangents();

        CombineInstance[] combineCustomMeshFilters = new CombineInstance[customMeshFilters.Count + 1];
        meshFilter.mesh = normalMesh;
        combineCustomMeshFilters[0].mesh = normalMesh;
        combineCustomMeshFilters[0].transform = transform.worldToLocalMatrix * transform.localToWorldMatrix;
        int i = 1;
        while (i < customMeshFilters.Count + 1)
        {
            combineCustomMeshFilters[i].mesh = customMeshFilters[i].sharedMesh;
            combineCustomMeshFilters[i].transform = customMeshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combineCustomMeshFilters);
        meshFilter.mesh = finalMesh;
        meshCollider.sharedMesh = finalMesh;
        // Set Texture

        requiresMeshGeneration = false;
    }

    void GenerateBlock_Top(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.y + 1 < sizeY && blockIDs[pos.x, pos.y + 1, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y + 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(0, 1, 0) + offset);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Right(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.x + 1 < sizeX && blockIDs[pos.x + 1, pos.y, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x + 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);

        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Left(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.x - 1 >= 0 && blockIDs[pos.x - 1, pos.y, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x - 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(0, 1, 0) + offset);
        vertices.Add(new Vector3(0, 0, 0) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);

        normals.Add(Vector3.left);
        normals.Add(Vector3.left);
        normals.Add(Vector3.left);
        normals.Add(Vector3.left);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Forward(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.z + 1 < sizeZ && blockIDs[pos.x, pos.y, pos.z + 1] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z + 1])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);

        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Back(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.z - 1 >= 0 && blockIDs[pos.x, pos.y, pos.z - 1] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z - 1])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 0) + offset);
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);
        vertices.Add(new Vector3(0, 0, 0) + offset);

        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Bottom(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos, List<MeshFilter> customMeshFilters)
    {
        if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
        {
            customMeshFilters.Add(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().customMeshFilter);
            return;
        }

        if (pos.y - 1 >= 0 && blockIDs[pos.x, pos.y - 1, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y - 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 0, 0) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);

        normals.Add(Vector3.down);
        normals.Add(Vector3.down);
        normals.Add(Vector3.down);
        normals.Add(Vector3.down);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    public bool RemoveBlock(Vector3Int pos, bool spawnItem = false, Vector3 spawnPos = default(Vector3), Quaternion spawnRotation = default(Quaternion))
    {
        if (blockIDs[pos.x, pos.y, pos.z] != 0)
        {
            if (spawnItem == true)
            {
                GameObject newItem;
                newItem = Instantiate(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])], spawnPos, spawnRotation);
                Item spawnedItem = newItem.GetComponent<Item>();
                spawnedItem.SetStackSize(1);
            }
            blockIDs[pos.x, pos.y, pos.z] = 0;
            requiresMeshGeneration = true;
            return true;
        }
        return false;
    }

    public bool AddBlock(Vector3Int pos, short blockID)
    {
        if (blockIDs[pos.x, pos.y, pos.z] == 0)
        {
            blockIDs[pos.x, pos.y, pos.z] = blockID;
            requiresMeshGeneration = true;
            return true;
        }
        return false;
    }

    public float GetStiffness(Vector3Int pos)
    {
        return itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().stiffness;
    }

    public Rect GetFaceTexture(short blockID, Faces face)
    {
        return new Rect((float)face * vertexLength / resolution[0], (float)blockID * vertexLength / resolution[1], (float)vertexLength / resolution[0], (float)vertexLength / resolution[1]);
    }
}