using UnityEngine;
using System.Collections.Generic;


public class RoadGenerator : MonoBehaviour
{
    public float spacing = 5f; // Distance between road pieces
    public Material roadMaterial; // Main "road" material
    public Material roadMaterial2; // Secondary "road" material for details
    public Material greenMaterial; // Green circuitboard material
    public Material buildingMaterial; // Gray building material
    public int[,] grid; // The grid/matrix that defines the road layout (0 or 1)
    public int gridSize = 20; // Size of the grid (20x20)
    public int seed = 0; // Random seed for generating the grid

    void Start()
    {
        Random.InitState(seed);
        grid = new int[gridSize, gridSize];
        // Initialize sets for each cell in the first row
        List<int>[] sets = new List<int>[gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            sets[i] = new List<int> { i };  // Each cell starts in its own set
            grid[0, i] = 1;  // Initialize first row with 1s (path)
        }

        // Eller's algorithm (thank you piazza for inspiration)
        for (int z = 0; z < gridSize - 1; z++)
        {
            // Merge adjacent cells in random sets (with a random chance)
            for (int x = 0; x < gridSize - 1; x++)
            {
                if (sets[x] != sets[x + 1] && Random.Range(0, 2) == 1)
                {
                    // Merge sets
                    sets[x + 1].ForEach(cell => sets[x].Add(cell));
                    sets[x + 1] = sets[x];
                    grid[z, x + 1] = 1;  // Create horizontal path
                }
            }

            // Create vertical connections to the next row (random)
            List<int>[] newSets = new List<int>[gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                if (Random.Range(0, 2) == 1 || sets[x].Count == 1)
                {
                    // Carry the current set to the next row
                    grid[z + 1, x] = 1;  // Create vertical path
                    newSets[x] = sets[x];
                }
                else
                {
                    newSets[x] = new List<int> { x + z * gridSize };  // New set
                }
            }
            sets = newSets;  // Move to the next row
        }

        // Handle the last row: merge all remaining sets
        for (int x = 0; x < gridSize - 1; x++)
        {
            if (sets[x] != sets[x + 1])
            {
                // Merge remaining sets
                sets[x + 1].ForEach(cell => sets[x].Add(cell));
                sets[x + 1] = sets[x];
                grid[gridSize - 1, x + 1] = 1;  // Create horizontal path
            }
        }

        // Generate road grid based on the matrix
        GenerateRoadGrid();
        PlaceBuildings();
    }

    bool IsWithinBounds(int x, int z)
    {
        return x >= 0 && x < gridSize && z >= 0 && z < gridSize;
    }

    void GenerateRoadGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Skip if the current tile is a 0 (no road)
                if (grid[x, z] == 0)
                    continue;

                // Get surrounding values (default to 0 if out of bounds)
                int up = (z < gridSize - 1) ? grid[x, z + 1] : 0;
                int down = (z > 0) ? grid[x, z - 1] : 0;
                int left = (x > 0) ? grid[x - 1, z] : 0;
                int right = (x < gridSize - 1) ? grid[x + 1, z] : 0;

                // Count the number of surrounding 1s
                int numOnes = up + down + left + right;

                GameObject roadPiece = null;

                // Determine the road piece based on surrounding tiles
                if (numOnes == 1)
                {
                    roadPiece = CreateDeadEnd();
                }
                else if (numOnes == 2)
                {
                    if ((up == 1 && down == 1) || (left == 1 && right == 1))
                    {
                        roadPiece = CreateStraightRoad(); // Opposite sides
                    }
                    else
                    {
                        roadPiece = CreateTurn(); // Adjacent sides
                    }
                }
                else if (numOnes == 3)
                {
                    roadPiece = CreateTJunction();
                }
                else if (numOnes == 4)
                {
                    roadPiece = CreateFourWayIntersection();
                }

                // Position and rotate the road piece in the grid
                if (roadPiece != null)
                {
                    Vector3 position = new Vector3(x * spacing, 0, z * spacing);
                    CreateRoadPiece(roadPiece, position);

                    // Call the rotation method to rotate based on neighboring ones
                    RotateRoadPiece(roadPiece, x, z, grid);
                }
            }
        }
    }


    // Helper method to place road pieces in the scene
    void CreateRoadPiece(GameObject roadPiece, Vector3 position)
    {
        roadPiece.transform.position = position;
        roadPiece.transform.parent = this.transform;

        MeshRenderer meshRenderer = roadPiece.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (roadMaterial != null)
            {
                meshRenderer.material = roadMaterial;
            }
            else
            {
                meshRenderer.material = new Material(Shader.Find("Standard"));
            }
        }
    }

    // Straight road piece
    GameObject CreateStraightRoad()
    {
        GameObject road = new GameObject("Road");
        GameObject road1 = CreateSquare(1);
        road1.transform.parent = road.transform;
        road1.transform.position = new Vector3(0, 0.02f, 0);
        road1.transform.localScale = new Vector3(1, 0.1f, 5);

        GameObject road2 = CreateSquare(1);
        road2.transform.parent = road.transform;
        road2.transform.position = new Vector3(0, 0.01f, 0);
        road2.transform.localScale = new Vector3(1.5f, 0.05f, 5);

        MeshRenderer renderer = road1.GetComponent<MeshRenderer>();
        if (renderer != null && roadMaterial != null)
        {
            renderer.material = roadMaterial;
        }

        MeshRenderer renderer2 = road2.GetComponent<MeshRenderer>();
        if (renderer2 != null && roadMaterial2 != null)
        {
            renderer2.material = roadMaterial2;
        }

        return road;
    }

    // Turn piece
    GameObject CreateTurn()
    {
        GameObject turn = new GameObject("Turn");

        GameObject road1 = CreateSquare(1);
        road1.transform.parent = turn.transform;
        road1.transform.localScale = new Vector3(1, 0.1f, 4.2f);
        road1.transform.position = new Vector3(1.25f, 0.02f, -1.25f);
        road1.transform.rotation = Quaternion.Euler(0, 45, 0);

        GameObject road2 = CreateSquare(1);
        road2.transform.parent = turn.transform;
        road2.transform.localScale = new Vector3(1.5f, 0.05f, 4.6f);
        road2.transform.position = new Vector3(1.25f, 0.01f, -1.25f);
        road2.transform.rotation = Quaternion.Euler(0, 45, 0);

        MeshRenderer renderer = road1.GetComponent<MeshRenderer>();
        if (renderer != null && roadMaterial != null)
        {
            renderer.material = roadMaterial;
        }

        MeshRenderer renderer2 = road2.GetComponent<MeshRenderer>();
        if (renderer2 != null && roadMaterial2 != null)
        {
            renderer2.material = roadMaterial2;
        }

        return turn;
    }

    // 4-way intersection
    GameObject CreateFourWayIntersection()
    {
        GameObject intersection = new GameObject("4-Way Intersection");

        GameObject road1 = CreateSquare(1);
        road1.transform.parent = intersection.transform;
        road1.transform.position = new Vector3(0, 0.02f, 0);
        road1.transform.localScale = new Vector3(1, 0.1f, 5);

        GameObject road2 = CreateSquare(1);
        road2.transform.parent = intersection.transform;
        road2.transform.position = new Vector3(0, 0.02f, 0);
        road2.transform.localScale = new Vector3(1, 0.1f, 5);
        road2.transform.rotation = Quaternion.Euler(0, 90, 0);

        GameObject road3 = CreateSquare(1);
        road3.transform.parent = intersection.transform;
        road3.transform.position = new Vector3(0, 0.01f, 0);
        road3.transform.localScale = new Vector3(1.5f, 0.05f, 5);

        GameObject road4 = CreateSquare(1);
        road4.transform.parent = intersection.transform;
        road4.transform.position = new Vector3(0, 0.01f, 0);
        road4.transform.localScale = new Vector3(1.5f, 0.05f, 5);
        road4.transform.rotation = Quaternion.Euler(0, 90, 0);

        MeshRenderer renderer = road1.GetComponent<MeshRenderer>();
        if (renderer != null && roadMaterial != null)
        {
            renderer.material = roadMaterial;
        }

        MeshRenderer renderer2 = road2.GetComponent<MeshRenderer>();
        if (renderer2 != null && roadMaterial != null)
        {
            renderer2.material = roadMaterial;
        }

        MeshRenderer renderer3 = road3.GetComponent<MeshRenderer>();
        renderer3.material = roadMaterial2;
        MeshRenderer renderer4 = road4.GetComponent<MeshRenderer>();
        renderer4.material = roadMaterial2;

        return intersection;
    }

    GameObject CreateTJunction()
    {
        GameObject tJunction = new GameObject("T-Junction");

        GameObject road1 = CreateSquare(1);
        road1.transform.parent = tJunction.transform;
        road1.transform.localScale = new Vector3(1, 0.1f, 2.5f);
        road1.transform.position = new Vector3(1.25f, 0.02f, 0);
        road1.transform.rotation = Quaternion.Euler(0, 90, 0);

        GameObject road2 = CreateSquare(1);
        road2.transform.parent = tJunction.transform;
        road2.transform.localScale = new Vector3(1, 0.1f, 5);
        road2.transform.position = new Vector3(0, 0.02f, 0);

        GameObject road3 = CreateSquare(1);
        road3.transform.parent = tJunction.transform;
        road3.transform.localScale = new Vector3(1.5f, 0.05f, 2.5f);
        road3.transform.position = new Vector3(1.25f, 0.01f, 0);
        road3.transform.rotation = Quaternion.Euler(0, 90, 0);

        GameObject road4 = CreateSquare(1);
        road4.transform.parent = tJunction.transform;
        road4.transform.localScale = new Vector3(1.5f, 0.05f, 5);
        road4.transform.position = new Vector3(0, 0.01f, 0);

        MeshRenderer renderer = road1.GetComponent<MeshRenderer>();
        if (renderer != null && roadMaterial != null)
        {
            renderer.material = roadMaterial;
        }

        MeshRenderer renderer2 = road2.GetComponent<MeshRenderer>();
        if (renderer2 != null && roadMaterial != null)
        {
            renderer2.material = roadMaterial;
        }

        MeshRenderer renderer3 = road3.GetComponent<MeshRenderer>();
        renderer3.material = roadMaterial2;
        MeshRenderer renderer4 = road4.GetComponent<MeshRenderer>();
        renderer4.material = roadMaterial2;

        return tJunction;
    }


    // Dead end
    GameObject CreateDeadEnd()
    {
        GameObject deadEnd = new GameObject("Dead End");

        GameObject road1 = CreateSquare(1);
        road1.transform.parent = deadEnd.transform;
        road1.transform.localScale = new Vector3(1, 0.1f, 2.5f);
        road1.transform.position = new Vector3(0, 0.02f, -1.25f);

        GameObject road2 = CreateSquare(1);
        road2.transform.parent = deadEnd.transform;
        road2.transform.localScale = new Vector3(1.5f, 0.05f, 2.5f);
        road2.transform.position = new Vector3(0, 0.01f, -1.25f);

        GameObject culDeSac = CreateCustomPolygon(1.5f, 32);
        culDeSac.transform.parent = deadEnd.transform;
        culDeSac.transform.localScale = new Vector3(1, 0.1f, 1);
        culDeSac.transform.position = new Vector3(0, 0.02f, 0);

        GameObject culDeSac2 = CreateCustomPolygon(1.5f, 32); // Creates a 32 vertex polygon for the black divots on dead end, method below
        culDeSac2.transform.parent = deadEnd.transform;
        culDeSac2.transform.localScale = new Vector3(0.5f, 10f, 0.5f);
        culDeSac2.transform.position = new Vector3(0, 0.025f, 0);


        GameObject culDeSac3 = CreateCustomPolygon(1.5f, 32);
        culDeSac3.transform.parent = deadEnd.transform;
        culDeSac3.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
        culDeSac3.transform.position = new Vector3(0, 0.01f, 0);

        // Materials
        MeshRenderer renderer = culDeSac2.GetComponent<MeshRenderer>();
        if (renderer != null && buildingMaterial != null)
        {
            renderer.material = buildingMaterial;
        }

        MeshRenderer renderer2 = culDeSac.GetComponent<MeshRenderer>();
        if (renderer2 != null && roadMaterial != null)
        {
            renderer2.material = roadMaterial;
        }

        MeshRenderer renderer3 = road1.GetComponent<MeshRenderer>();
        if (renderer3 != null && roadMaterial != null)
        {
            renderer3.material = roadMaterial;
        }

        MeshRenderer renderer4 = culDeSac3.GetComponent<MeshRenderer>();
        if (renderer4 != null && roadMaterial2 != null)
        {
            renderer4.material = roadMaterial2;
        }

        MeshRenderer renderer5 = road2.GetComponent<MeshRenderer>();
        if (renderer5 != null && roadMaterial2 != null)
        {
            renderer5.material = roadMaterial2;
        }

        return deadEnd;
    }

    GameObject CreateCustomPolygon(float radius, int segments) // Used to make any flat polygon of any vertex count
    {
        GameObject polygon = new GameObject("Custom Polygon");

        MeshFilter meshFilter = polygon.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = polygon.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Use lists to handle vertices and triangles
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        float angleIncrement = 2 * Mathf.PI / segments;

        // Add vertices for the polygon edge
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleIncrement;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            // Reverse the order of the vertices for correct facing
            vertices.Add(new Vector3(x, 0, z)); // Edge vertex in XZ plane
            normals.Add(Vector3.up); // Normal pointing up
        }

        // Center vertex
        vertices.Add(Vector3.zero); // Center point of the polygon
        normals.Add(Vector3.up);    // Normal for center pointing up

        // Create triangles with reversed order
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(segments);    // Center point
            triangles.Add((i + 1) % segments); // Next edge point (wrap around to first)
            triangles.Add(i);           // Current edge point
        }

        // Apply the mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        mesh.RecalculateBounds(); // Recalculate bounds for optimization
        mesh.RecalculateNormals(); // Recalculate normals for correct lighting

        meshFilter.mesh = mesh;

        MeshCollider meshCollider = polygon.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        // Apply material
        if (buildingMaterial != null)
        {
            meshRenderer.material = buildingMaterial;
        }

        return polygon;
    }


    GameObject CreateSquare(float size) // Used to create a square in the XZ plane
    {
        GameObject square = new GameObject("Square");

        MeshFilter meshFilter = square.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = square.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Define vertices for a square in the XZ plane
        List<Vector3> vertices = new List<Vector3>
    {
        new Vector3(-size / 2, 0, -size / 2), // Bottom-left
        new Vector3(size / 2, 0, -size / 2),  // Bottom-right
        new Vector3(size / 2, 0, size / 2),   // Top-right
        new Vector3(-size / 2, 0, size / 2)   // Top-left
    };

        // Define triangles (2 triangles to form a square)
        List<int> triangles = new List<int>
    {
        0, 2, 1, // First triangle (bottom-left, top-right, bottom-right)
        0, 3, 2  // Second triangle (bottom-left, top-left, top-right)
    };

        // Define normals (all facing upwards)
        List<Vector3> normals = new List<Vector3>
    {
        Vector3.up, // Normal for bottom-left vertex
        Vector3.up, // Normal for bottom-right vertex
        Vector3.up, // Normal for top-right vertex
        Vector3.up  // Normal for top-left vertex
    };

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        MeshCollider meshCollider = square.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        return square;
    }





    void RotateRoadPiece(GameObject roadPiece, int x, int y, int[,] grid)
    {
        // Check if the road piece is a straight road
        bool IsStraightRoad(bool top, bool bottom, bool left, bool right)
        {
            return (top && bottom && !left && !right) || (left && right && !top && !bottom);
        }

        // Check if the road piece is a turn
        bool IsTurn(bool top, bool bottom, bool left, bool right)
        {
            return (top && right && !left && !bottom) || (top && left && !right && !bottom) ||
                   (bottom && right && !left && !top) || (bottom && left && !right && !top);
        }

        // Check if the road piece is a T-junction
        bool IsTJunction(bool top, bool bottom, bool left, bool right)
        {
            return (top && bottom && left && !right) || (top && bottom && right && !left) ||
                   (top && left && right && !bottom) || (bottom && left && right && !top);
        }

        // Check if the road piece is a dead-end
        bool IsDeadEnd(bool top, bool bottom, bool left, bool right)
        {
            return (top && !bottom && !left && !right) ||
                   (bottom && !top && !left && !right) ||
                   (left && !top && !bottom && !right) ||
                   (right && !top && !bottom && !left);
        }
        // Check neighbors (1 = road, 0 = no road)
        bool top = y < grid.GetLength(1) - 1 && grid[x, y + 1] == 1;
        bool bottom = y > 0 && grid[x, y - 1] == 1;
        bool left = x > 0 && grid[x - 1, y] == 1;
        bool right = x < grid.GetLength(0) - 1 && grid[x + 1, y] == 1;

        // Default rotation (e.g., no rotation needed)
        Quaternion rotation = Quaternion.identity;

        // Rotate straight roads
        if (IsStraightRoad(top, bottom, left, right))
        {
            if (left && right) // Horizontal
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
        }
        // Rotate turn roads
        else if (IsTurn(top, bottom, left, right))
        {
            if (bottom && right) // Default turn
            {
                rotation = Quaternion.identity;
            }
            else if (bottom && left) // Rotate 90 degrees
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (top && left) // Rotate 180 degrees
            {
                rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (top && right) // Rotate 270 degrees
            {
                rotation = Quaternion.Euler(0, 270, 0);
            }
        }
        // Rotate T-junctions
        else if (IsTJunction(top, bottom, left, right))
        {
            if (top && bottom && left && !right) // No right
            {
                rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (top && bottom && right && !left) // No left
            {
                rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (top && left && right && !bottom) // No bottom
            {
                rotation = Quaternion.Euler(0, 270, 0);
            }
            else if (bottom && left && right && !top) // No top
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
        }

        // Rotate dead-end
        else if (IsDeadEnd(top, bottom, left, right))
        {
            if (top && !bottom && !left && !right) // Rotate 180 degrees
            {
                rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (!top && !bottom && left && !right) // Rotate 90 degrees
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (!top && !bottom && !left && right) // Rotate 270 degrees
            {
                rotation = Quaternion.Euler(0, 270, 0);
            }
        }

        // Apply the rotation to the road piece
        roadPiece.transform.rotation = rotation;
    }


    void PlaceBuildings()
    {
        int buildingCount = 0;
        List<Vector2Int> gridIndices = new List<Vector2Int>();

        // Fill grid indices with coordinates (x, z)
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                gridIndices.Add(new Vector2Int(x, z));
            }
        }

        // Shuffle the grid indices to randomize the order of checking spots
        ShuffleGridIndices(gridIndices);

        // Iterate through the shuffled grid indices
        foreach (Vector2Int index in gridIndices)
        {
            int x = index.x;
            int z = index.y;

            // Stop once we've placed 6 buildings
            if (buildingCount >= 6)
                return;

            // Skip non-road tiles
            if (grid[x, z] != 1)
                continue;

            // Check for empty spaces (0) adjacent to the road in random order
            List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(x + 1, z), // Right
            new Vector2Int(x - 1, z), // Left
            new Vector2Int(x, z + 1), // Top
            new Vector2Int(x, z - 1)  // Bottom
        };

            ShuffleGridIndices(neighbors); // Shuffle neighbors to randomize building placement

            foreach (Vector2Int neighbor in neighbors)
            {
                if (IsValidBuildingSpot(neighbor.x, neighbor.y))
                {
                    PlaceBuilding(neighbor.x, neighbor.y);
                    buildingCount++;
                    break; // Move to the next road tile after placing one building
                }
            }
        }
    }

    // Helper function to shuffle a list of grid indices
    void ShuffleGridIndices(List<Vector2Int> gridIndices)
    {
        for (int i = 0; i < gridIndices.Count; i++)
        {
            Vector2Int temp = gridIndices[i];
            int randomIndex = Random.Range(i, gridIndices.Count);
            gridIndices[i] = gridIndices[randomIndex];
            gridIndices[randomIndex] = temp;
        }
    }

    // Check if the spot is valid (within bounds and empty)
    bool IsValidBuildingSpot(int x, int z)
    {
        return x >= 0 && x < gridSize && z >= 0 && z < gridSize && grid[x, z] == 0;
    }

    // Place a simple building at the specified grid coordinates
    void PlaceBuilding(int x, int z)
    {
        Vector3 position = new Vector3(x * spacing, 0, z * spacing);

        // Create a building
        GameObject building = new GameObject("Building");
        GameObject building1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building1.transform.parent = building.transform;
        building1.transform.localScale = new Vector3(4, 2, 4);
        building1.transform.position = position;
        //building1.transform.parent = this.transform;

        GameObject building2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building2.transform.parent = building.transform;
        building2.transform.localScale = new Vector3(4.5f, 0.5f, 4.5f);
        building2.transform.position = position;

        if (roadMaterial != null)
        {
            MeshRenderer meshRenderer = building1.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = buildingMaterial;
            }
        }

        MeshRenderer meshRenderer2 = building2.GetComponent<MeshRenderer>();
        meshRenderer2.material = roadMaterial2;

        // Mark this spot in the grid as occupied by a building
        grid[x, z] = 2; // Mark with a different value for buildings
    }
}