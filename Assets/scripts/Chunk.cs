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

public class Chunk : MonoBehaviour
{
    public Land parentLand;
    public ItemPrefabs itemPrefabs;
    public BlockToItemID blockToItemID;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public short[,,] blockIDs;
    public bool requiresMeshGeneration = false;
    public int vertexLength;
    public Vector2Int resolution;
    public Dictionary<Vector3Int, Block> customBlocks = new Dictionary<Vector3Int, Block>();

    static public Vector3Int FaceToDirection(Faces face)
    {
        switch (face)
        {
            case Faces.Up: return Vector3Int.up;
            case Faces.Down: return Vector3Int.down;
            case Faces.Right: return Vector3Int.right;
            case Faces.Left: return Vector3Int.left;
            case Faces.Front: return Vector3Int.forward;
        }
        return Vector3Int.back;
    }

    static public Faces GetOppositeFace(Faces face)
    {
        switch (face)
        {
            case Faces.Up: return Faces.Down;
            case Faces.Down: return Faces.Up;
            case Faces.Right: return Faces.Left;
            case Faces.Left: return Faces.Right;
            case Faces.Front: return Faces.Back;
        }
        return Faces.Front;
    }

    public void WakeUp()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        blockIDs = new short[sizeX, sizeY, sizeZ];

        requiresMeshGeneration = true;
    }

    private void Update()
    {
        if (requiresMeshGeneration)
        {
            GenerateMesh();
            requiresMeshGeneration = false;
        }
    }

    void GenerateMesh()
    {
        // generate MeshFilter
        Mesh newMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int currentIndex = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (blockIDs[x, y, z] == 0) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
                    {
                        Vector3Int offset = new Vector3Int(x, y, z);
                        GenerateBlock_Top(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Up), pos);
                        GenerateBlock_Right(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Right), pos);
                        GenerateBlock_Left(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Left), pos);
                        GenerateBlock_Forward(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Front), pos);
                        GenerateBlock_Back(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Back), pos);
                        GenerateBlock_Bottom(ref currentIndex, offset, vertices, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Down), pos);
                    }
                }
            }
        }

        newMesh.SetVertices(vertices);
        newMesh.SetUVs(0, uvs);
        newMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        newMesh.RecalculateNormals();
        newMesh.RecalculateTangents();
        meshFilter.mesh = newMesh;

        //Delete old colliders
        while (gameObject.GetComponent<BoxCollider>() != null)
            DestroyImmediate(gameObject.GetComponent<BoxCollider>());

        //Generate new colliders
        bool[,,] availableBlocks = new bool[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                    if (blockIDs[x, y, z] != 0)
                        availableBlocks[x, y, z] = true;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (!availableBlocks[x, y, z])
                        continue;

                    int boxScaleX = 1, boxScaleY = 1, boxScaleZ = 1;
                    while (y + boxScaleY < sizeY && availableBlocks[x, y + boxScaleY, z])
                    {
                        availableBlocks[x, y + boxScaleY, z] = false;
                        boxScaleY++;
                    }
                    while (x + boxScaleX < sizeX)
                    {
                        bool breakLoop = false;
                        for (int i = 0; i < boxScaleY; i++)
                        {
                            if (!availableBlocks[x + boxScaleX, y + i, z])
                            {
                                breakLoop = true;
                                break;
                            }
                        }
                        if (breakLoop == true)
                            break;

                        for (int i = 0; i < boxScaleY; i++)
                        {
                            availableBlocks[x + boxScaleX, y + i, z] = false;
                        }
                        boxScaleX++;
                    }
                    while (z + boxScaleZ < sizeZ)
                    {
                        bool breakLoop = false;
                        for (int i = 0; i < boxScaleY; i++)
                        {
                            for (int j = 0; j < boxScaleX; j++)
                            {
                                if (!availableBlocks[x + j, y + i, z + boxScaleZ])
                                {
                                    breakLoop = true;
                                    break;
                                }
                            }
                        }
                        if (breakLoop == true)
                            break;

                        for (int i = 0; i < boxScaleY; i++)
                        {
                            for (int j = 0; j < boxScaleX; j++)
                            {
                                availableBlocks[x + j, y + i, z + boxScaleZ] = false;
                            }
                        }
                        boxScaleZ++;
                    }

                    BoxCollider newBoxCollider = gameObject.AddComponent<BoxCollider>();
                    newBoxCollider.size = new Vector3(boxScaleX, boxScaleY, boxScaleZ);
                    newBoxCollider.center = new Vector3(x + boxScaleX / 2f, y + boxScaleY / 2f, z + boxScaleZ / 2f);
                }
            }
        }

        requiresMeshGeneration = false;
    }

    void GenerateBlock_Top(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        //manyake
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.up);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(0, 1, 1) + offset);
        mainVertices.Add(new Vector3(1, 1, 1) + offset);
        mainVertices.Add(new Vector3(1, 1, 0) + offset);
        mainVertices.Add(new Vector3(0, 1, 0) + offset);
        Vector2 midUV = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3(midUV.x, Random.Range(0.9f, 1.2f), midUV.y) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin + (blockUVs.xMax - blockUVs.xMin) * midUV.x, blockUVs.yMin + (blockUVs.yMax - blockUVs.yMin) * midUV.y));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[3]);

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[3]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    void GenerateBlock_Right(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.right);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(1, 1, 0) + offset);
        mainVertices.Add(new Vector3(1, 1, 1) + offset);
        mainVertices.Add(new Vector3(1, 0, 1) + offset);
        mainVertices.Add(new Vector3(1, 0, 0) + offset);
        Vector2 midUV = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3(Random.Range(0.9f, 1.2f), midUV.x, midUV.y) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin + (blockUVs.xMax - blockUVs.xMin) * midUV.y, blockUVs.yMin + (blockUVs.yMax - blockUVs.yMin) * midUV.x));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[3]);
        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[2]);
        uvs.Add(mainUvs[3]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[2]);

        vertices.Add(mainVertices[3]);
        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    void GenerateBlock_Left(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.left);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(0, 1, 1) + offset);
        mainVertices.Add(new Vector3(0, 1, 0) + offset);
        mainVertices.Add(new Vector3(0, 0, 0) + offset);
        mainVertices.Add(new Vector3(0, 0, 1) + offset);
        Vector2 midVertex = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3(Random.Range(-0.2f, 0.1f), midVertex.x, midVertex.y) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMax - (blockUVs.xMax - blockUVs.xMin) * midVertex.y, blockUVs.yMin + (blockUVs.yMax - blockUVs.yMin) * midVertex.x));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[3]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[3]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[3]);
        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    void GenerateBlock_Forward(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.forward);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(1, 1, 1) + offset);
        mainVertices.Add(new Vector3(0, 1, 1) + offset);
        mainVertices.Add(new Vector3(0, 0, 1) + offset);
        mainVertices.Add(new Vector3(1, 0, 1) + offset);
        Vector2 midUV = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3( midUV.x, midUV.y, Random.Range(0.9f, 1.2f)) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMax - (blockUVs.xMax - blockUVs.xMin) * midUV.x, blockUVs.yMin + (blockUVs.yMax - blockUVs.yMin) * midUV.y));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[3]);

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[3]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    void GenerateBlock_Back(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.back);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(0, 1, 0) + offset);
        mainVertices.Add(new Vector3(1, 1, 0) + offset);
        mainVertices.Add(new Vector3(1, 0, 0) + offset);
        mainVertices.Add(new Vector3(0, 0, 0) + offset);
        Vector2 midUV = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3(midUV.x, midUV.y, Random.Range(-0.2f, 0.1f)) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin + (blockUVs.xMax - blockUVs.xMin) * midUV.x, blockUVs.yMin + (blockUVs.yMax - blockUVs.yMin) * midUV.y));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[3]);

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[3]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    void GenerateBlock_Bottom(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        short neighborBlockID = parentLand.GetBlockID(Vector3Int.FloorToInt(transform.localPosition) + pos + Vector3Int.down);
        if (neighborBlockID != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(neighborBlockID)].GetComponent<Block>().hasCustomMesh) return;
        List<Vector3> mainVertices = new List<Vector3>();
        mainVertices.Add(new Vector3(0, 0, 0) + offset);
        mainVertices.Add(new Vector3(1, 0, 0) + offset);
        mainVertices.Add(new Vector3(1, 0, 1) + offset);
        mainVertices.Add(new Vector3(0, 0, 1) + offset);
        Vector2 midUV = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        mainVertices.Add(new Vector3(midUV.x, Random.Range(-0.2f, 0.1f), midUV.y) + offset);

        List<Vector2> mainUvs = new List<Vector2>();
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        mainUvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        mainUvs.Add(new Vector2(blockUVs.xMin + (blockUVs.xMax - blockUVs.xMin) * midUV.x, blockUVs.yMax - (blockUVs.yMax - blockUVs.yMin) * midUV.y));

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[1]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[4]);
        uvs.Add(mainUvs[1]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[4]);

        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[2]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[2]);
        uvs.Add(mainUvs[3]);

        vertices.Add(mainVertices[0]);
        vertices.Add(mainVertices[4]);
        vertices.Add(mainVertices[3]);
        uvs.Add(mainUvs[0]);
        uvs.Add(mainUvs[4]);
        uvs.Add(mainUvs[3]);

        for (int i = 0; i < 4; i++)
        {
            indices.Add(currentIndex);
            indices.Add(currentIndex + 1);
            indices.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    public bool RemoveBlock(Vector3Int pos, bool spawnItem = false)
    {
        if (blockIDs[pos.x, pos.y, pos.z] != 0)
        {
            if (customBlocks.ContainsKey(pos))
            {
                Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                customBlocks[pos].BreakCustomBlock(spawnPos, spawnItem);
                customBlocks.Remove(pos);
            }
            else
            {
                if (spawnItem == true)
                {
                    GameObject newItem;
                    Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                    newItem = Instantiate(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])], spawnPos, default(Quaternion));
                    Item spawnedItem = newItem.GetComponent<Item>();
                    spawnedItem.SetStackSize(1);
                }
                requiresMeshGeneration = true;
            }

            blockIDs[pos.x, pos.y, pos.z] = 0;
            return true;
        }
        return false;
    }

    public bool AddBlock(Vector3Int landPos, short blockID, Quaternion rotation = default, bool generateMesh = true)
    {
        Vector3Int pos = new Vector3Int(landPos.x % sizeX, landPos.y % sizeY, landPos.z % sizeZ);
        if (blockIDs[pos.x, pos.y, pos.z] == 0)
        {
            if (blockID == 0) return true;

            blockIDs[pos.x, pos.y, pos.z] = blockID;
            if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
            {
                Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                Block customBlock = (Block)itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().PlaceCustomBlock(spawnPos, rotation, this, landPos);
                customBlocks.Add(pos, customBlock);
            }
            else if (generateMesh)
            {
                requiresMeshGeneration = true;
            }
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

    public Block GetCustomBlock(Vector3Int pos)
    {
        if (customBlocks.ContainsKey(pos))
            return customBlocks[pos];
        else
            return null;
    }
}