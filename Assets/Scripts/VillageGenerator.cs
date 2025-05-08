using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using skner.DualGrid;
using UnityEngine.Tilemaps;
using System.Linq;

public class VillageGenerator : MonoBehaviour
{
    [Header("Building Prefabs")]
    public GameObject housePrefab;
    
    [Header("Path Settings")]
    public DualGridRuleTile pathTile;
    [Range(1, 3)]
    public int pathWidth = 1;
    
    [Header("Village Layout")]
    [Range(2, 10)]
    public int minHouseDistance = 5;
    [Range(3, 20)]
    public int maxHouses = 8;
    
    [Header("House Settings")]
    public int houseWidth = 3;
    public int houseHeight = 2;
    
    [Header("Path Optimization")]
    [Range(0, 100)]
    public int turnCostMultiplier = 10;
    
    // References
    private WorldHandler worldHandler;
    private Tilemap tilemap;
    
    // Tracking data
    private HashSet<Vector3Int> pathTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private List<HouseData> houses = new List<HouseData>();
    
    // House data structure
    private class HouseData
    {
        public Vector3Int Position;
        public GameObject Instance;
        public HashSet<Vector3Int> Footprint = new HashSet<Vector3Int>();
        public int Width;
        public int Height;
        
        public List<Vector3Int> GetPathConnectionPoints()
        {
            List<Vector3Int> connectionPoints = new List<Vector3Int>();
            int startX = Position.x - Width / 2;
            int endX = startX + Width;
            int y = Position.y - 1; // Path connects at the bottom of the house
            
            for (int x = startX; x < endX; x++)
            {
                connectionPoints.Add(new Vector3Int(x, y, Position.z));
            }
            
            return connectionPoints;
        }
    }
    
    private void Start()
    {
        InitializeReferences();
    }
    
    private void InitializeReferences()
    {
        worldHandler = FindObjectOfType<WorldHandler>();
        
        if (worldHandler == null)
        {
            Debug.LogError("WorldHandler not found in the scene!");
            return;
        }
        
        if (worldHandler.dualGridTilemaps.Length > 0)
        {
            tilemap = worldHandler.dualGridTilemaps[0];
        }
        else
        {
            Debug.LogError("No tilemaps found in WorldHandler!");
        }
    }
    
    public void GenerateVillage(List<Vector3Int> grassPositions, int houseCount = -1)
    {
        CleanUp();
        
        if (grassPositions == null || grassPositions.Count == 0)
        {
            Debug.LogError("No valid grass positions for village generation!");
            return;
        }
        
        if (tilemap == null || worldHandler == null)
        {
            InitializeReferences();
            if (tilemap == null || worldHandler == null)
            {
                Debug.LogError("Required references not found!");
                return;
            }
        }
        
        // FIXED: Use the smaller of the passed count or maxHouses
        int housesToGenerate = houseCount > 0 ? Mathf.Min(houseCount, maxHouses) : maxHouses;
        
        // Only apply an absolute cap if maxHouses is unreasonably high
        if (maxHouses > 12)
        {
            housesToGenerate = Mathf.Min(housesToGenerate, 12);
        }
        
        HashSet<Vector3Int> validTileSet = new HashSet<Vector3Int>(grassPositions);
        
        Debug.Log($"Starting village generation with exactly {housesToGenerate} houses (maxHouses: {maxHouses})");
        Debug.Log($"Valid tile count: {validTileSet.Count}");
        
        // Reset house list to ensure we start fresh
        houses.Clear();
        
        // 1. Generate houses with proper constraints
        PlaceHousesWithConstraints(validTileSet, housesToGenerate);
        
        // 2. Connect houses with paths only if we have houses
        if (houses.Count > 0)
        {
            ConnectHousesWithPaths(validTileSet);
            
            // 3. Apply path tiles
            ApplyPaths(validTileSet);
        }
        
        Debug.Log($"Village generation complete: {houses.Count} houses and {pathTiles.Count} path tiles");
    }
    
    private void CleanUp()
    {
        // Remove any existing houses
        foreach (var house in houses)
        {
            if (house.Instance != null)
            {
                Destroy(house.Instance);
            }
        }
        
        houses.Clear();
        pathTiles.Clear();
        occupiedTiles.Clear();
    }
    
    private void PlaceHousesWithConstraints(HashSet<Vector3Int> validTiles, int targetHouseCount)
    {
        // If we somehow already have houses, clear them
        if (houses.Count > 0)
        {
            Debug.Log("Clearing existing houses before placement");
            CleanUp();
        }
        
        Debug.Log($"Attempting to place EXACTLY {targetHouseCount} houses");
        
        // Find center of available area as starting point
        Vector3Int center = CalculateMidpoint(validTiles.ToList());
        
        // For larger maps, ensure we use a reasonable min distance between houses
        int effectiveMinDistance = Mathf.Max(minHouseDistance, 5);
        Debug.Log($"Using minimum house distance: {effectiveMinDistance}");
        
        // Try to place first house at center
        bool firstHousePlaced = TryPlaceHouseAt(center, validTiles);
        
        // If center placement failed, try nearby positions in a spiral pattern
        if (!firstHousePlaced)
        {
            for (int r = 1; r <= 10 && !firstHousePlaced; r++)
            {
                for (int dx = -r; dx <= r && !firstHousePlaced; dx++)
                {
                    for (int dy = -r; dy <= r && !firstHousePlaced; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        
                        Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, center.z);
                        firstHousePlaced = TryPlaceHouseAt(pos, validTiles);
                    }
                }
            }
        }
        
        if (houses.Count == 0)
        {
            Debug.LogError("Failed to place any houses - no suitable location found!");
            return;
        }
        
        // Use a modified Poisson disk sampling approach
        List<Vector3Int> activePoints = new List<Vector3Int> { houses[0].Position };
        int attempts = 0;
        int maxAttempts = 1000; // Hard safety limit
        
        while (houses.Count < targetHouseCount && activePoints.Count > 0 && attempts < maxAttempts)
        {
            attempts++;
            
            // Pick a random active point
            int index = Random.Range(0, activePoints.Count);
            Vector3Int current = activePoints[index];
            bool placedNewHouse = false;
            
            // Try placement at different angles around this point
            for (int i = 0; i < 12 && !placedNewHouse && houses.Count < targetHouseCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / 12f);
                float distance = Random.Range(effectiveMinDistance, effectiveMinDistance * 1.5f);
                
                int newX = current.x + Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                int newY = current.y + Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                
                Vector3Int newPos = new Vector3Int(newX, newY, center.z);
                
                if (TryPlaceHouseAt(newPos, validTiles))
                {
                    activePoints.Add(newPos);
                    placedNewHouse = true;
                    Debug.Log($"Placed house {houses.Count} of {targetHouseCount}");
                }
            }
            
            // If we couldn't place a new house from this point, remove it from active list
            if (!placedNewHouse)
            {
                activePoints.RemoveAt(index);
            }
            
            // Strict enforcement of house limit
            if (houses.Count >= targetHouseCount)
            {
                Debug.Log($"REACHED TARGET HOUSE COUNT: {houses.Count} of {targetHouseCount}");
                break;
            }
        }
        
        Debug.Log($"House placement finished: {houses.Count} of {targetHouseCount} placed with {attempts} attempts");
    }
    
    private void PlaceHousesPoisson(HashSet<Vector3Int> validTiles, int maxHouses)
    {
        // STRICT enforcement of house limit
        int housesToPlace = Mathf.Min(maxHouses, 12);
        Debug.Log($"Capped house count to {housesToPlace}");
        
        // If we somehow already have enough houses, skip placement
        if (houses.Count >= housesToPlace)
        {
            Debug.LogWarning($"Already have {houses.Count} houses, skipping placement");
            return;
        }
        
        // Adjust spacing based on house count to create better villages
        float spacingMultiplier = Mathf.Lerp(1.0f, 2.0f, housesToPlace / 12.0f);
        int effectiveMinDistance = Mathf.RoundToInt(minHouseDistance * spacingMultiplier);
        
        // Find center of available area as starting point
        Vector3Int center = CalculateMidpoint(validTiles.ToList());
        
        // Try to place first house at/near center
        bool placed = TryPlaceHouseAt(center, validTiles);
        if (!placed)
        {
            for (int r = 1; r <= 5 && !placed; r++)
            {
                for (int dx = -r; dx <= r && !placed; dx++)
                {
                    for (int dy = -r; dy <= r && !placed; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        
                        Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, center.z);
                        placed = TryPlaceHouseAt(pos, validTiles);
                        
                        // Check early to avoid unexpected behavior
                        if (houses.Count >= housesToPlace) {
                            Debug.Log($"Reached house limit during initial placement");
                            return;
                        }
                    }
                }
            }
        }
        
        if (houses.Count == 0)
        {
            Debug.LogError("Failed to place initial house - no suitable location found!");
            return;
        }
        
        // Use Poisson disk sampling for remaining houses
        List<Vector3Int> activePoints = new List<Vector3Int> { houses[0].Position };
        
        // Use a fixed attempt count to avoid infinite looping
        int totalAttempts = 0;
        int maxTotalAttempts = 500; // Safety limit
        
        // Stop when we reach the exact number of houses requested OR hit attempt limit
        while (activePoints.Count > 0 && houses.Count < housesToPlace && totalAttempts < maxTotalAttempts)
        {
            int randomIndex = Random.Range(0, activePoints.Count);
            Vector3Int current = activePoints[randomIndex];
            bool foundNewPosition = false;
            
            // Try up to 20 times to place a house around this point
            for (int attempt = 0; attempt < 20 && !foundNewPosition && houses.Count < housesToPlace; attempt++)
            {
                totalAttempts++;
                
                float angle = Random.Range(0f, Mathf.PI * 2f);
                // Scale distance with house count - smaller villages have tighter clustering
                float distance = Random.Range(effectiveMinDistance, effectiveMinDistance * 1.5f);
                
                int newX = current.x + Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                int newY = current.y + Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                
                Vector3Int newPos = new Vector3Int(newX, newY, current.z);
                
                if (TryPlaceHouseAt(newPos, validTiles))
                {
                    // Enforce the limit strictly
                    if (houses.Count >= housesToPlace) {
                        Debug.Log($"Reached house limit of {housesToPlace} (strict check)");
                        return;
                    }
                    
                    activePoints.Add(newPos);
                    foundNewPosition = true;
                }
            }
            
            // If no valid position found after all attempts, remove this point
            if (!foundNewPosition)
            {
                activePoints.RemoveAt(randomIndex);
            }
            
            // One more check for safety
            if (houses.Count >= housesToPlace) {
                Debug.Log($"Reached house limit of {housesToPlace} (loop exit check)");
                return;
            }
        }
        
        if (totalAttempts >= maxTotalAttempts) {
            Debug.LogWarning($"Hit max attempts ({maxTotalAttempts}) during house placement");
        }
        
        Debug.Log($"Placed {houses.Count} houses of {housesToPlace} requested");
    }
    
    private bool TryPlaceHouseAt(Vector3Int position, HashSet<Vector3Int> validTiles)
    {
        if (CanPlaceHouse(position, validTiles))
        {
            PlaceHouse(position);
            return true;
        }
        return false;
    }
    
    private bool CanPlaceHouse(Vector3Int position, HashSet<Vector3Int> validTiles)
    {
        // Check if all required tiles are valid and unoccupied
        int startX = position.x - houseWidth / 2;
        int endX = startX + houseWidth;
        int startY = position.y;
        int endY = position.y + houseHeight;
        
        // Check house area
        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, position.z);
                if (!validTiles.Contains(pos) || occupiedTiles.Contains(pos))
                {
                    return false;
                }
            }
        }
        
        // Check area for paths below house
        for (int x = startX; x < endX; x++)
        {
            Vector3Int pathPos = new Vector3Int(x, position.y - 1, position.z);
            if (!validTiles.Contains(pathPos))
            {
                return false;
            }
        }
        
        // Check minimum distance from other houses
        foreach (var house in houses)
        {
            float distance = Vector3Int.Distance(position, house.Position);
            if (distance < minHouseDistance)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void PlaceHouse(Vector3Int position)
    {
        // Convert to world position with proper dual grid offset
        Vector3 worldPos = tilemap.GetCellCenterWorld(position);
        
        // Add the 0.5,0.5 offset required for dual grid alignment
        worldPos.x += 0.5f;
        worldPos.x -= 1f;
        worldPos.y += 0.5f;
        worldPos.z = -1f; // Ensure house is above ground
        
        // Instantiate house prefab
        GameObject houseInstance = Instantiate(housePrefab, worldPos, Quaternion.identity);
        
        // Set sprite sorting order
        SpriteRenderer renderer = houseInstance.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10;
        }
        
        // Create house data
        HouseData house = new HouseData
        {
            Position = position,
            Instance = houseInstance,
            Width = houseWidth,
            Height = houseHeight
        };
        
        // Mark occupied tiles
        int startX = position.x - houseWidth / 2;
        int startY = position.y;
        
        for (int x = 0; x < houseWidth; x++)
        {
            for (int y = 0; y < houseHeight; y++)
            {
                Vector3Int pos = new Vector3Int(startX + x, startY + y, position.z);
                house.Footprint.Add(pos);
                occupiedTiles.Add(pos);
            }
        }
        
        houses.Add(house);
    }
    
    private void ConnectHousesWithPaths(HashSet<Vector3Int> validTiles)
    {
        if (houses.Count < 2) return;
        
        // Use minimum spanning tree to determine house connections
        List<(HouseData, HouseData)> connections = CreateMinimumSpanningTree();
        
        // Connect each pair of houses
        foreach (var (houseA, houseB) in connections)
        {
            ConnectHouses(houseA, houseB, validTiles);
        }
    }
    
    private void ConnectHouses(HouseData houseA, HouseData houseB, HashSet<Vector3Int> validTiles)
    {
        // Get connection points for both houses
        List<Vector3Int> pointsA = houseA.GetPathConnectionPoints();
        List<Vector3Int> pointsB = houseB.GetPathConnectionPoints();
        
        // Find closest pair of connection points
        Vector3Int bestStart = pointsA[0];
        Vector3Int bestEnd = pointsB[0];
        float bestDistance = float.MaxValue;
        
        foreach (var start in pointsA)
        {
            foreach (var end in pointsB)
            {
                float distance = Vector3Int.Distance(start, end);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestStart = start;
                    bestEnd = end;
                }
            }
        }
        
        // Find path between the connection points
        List<Vector3Int> path = FindPath(bestStart, bestEnd, validTiles);
        
        // Add path tiles
        foreach (var point in path)
        {
            if (!occupiedTiles.Contains(point) || pathTiles.Contains(point))
            {
                pathTiles.Add(point);
            }
        }
        
        // Connect all building entrance points with straight paths
        ConnectBuildingEntrances(pointsA, validTiles);
        ConnectBuildingEntrances(pointsB, validTiles);
    }
    
    private void ConnectBuildingEntrances(List<Vector3Int> entrancePoints, HashSet<Vector3Int> validTiles)
    {
        if (entrancePoints.Count <= 1) return;
        
        // Connect adjacent entrance points with straight paths
        for (int i = 0; i < entrancePoints.Count - 1; i++)
        {
            Vector3Int current = entrancePoints[i];
            Vector3Int next = entrancePoints[i + 1];
            
            if (validTiles.Contains(current) && validTiles.Contains(next))
            {
                pathTiles.Add(current);
                pathTiles.Add(next);
            }
        }
    }
    
    private List<(HouseData, HouseData)> CreateMinimumSpanningTree()
    {
        List<(HouseData, HouseData)> connections = new List<(HouseData, HouseData)>();
        
        if (houses.Count < 2) return connections;
        
        // Start with one house
        HashSet<HouseData> connected = new HashSet<HouseData> { houses[0] };
        HashSet<HouseData> unconnected = new HashSet<HouseData>(houses);
        unconnected.Remove(houses[0]);
        
        // Keep connecting closest pairs until all houses are connected
        while (unconnected.Count > 0)
        {
            float minDistance = float.MaxValue;
            HouseData bestConnected = null;
            HouseData bestUnconnected = null;
            
            // Find closest pair between connected and unconnected sets
            foreach (var c in connected)
            {
                foreach (var u in unconnected)
                {
                    float distance = Vector3Int.Distance(c.Position, u.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestConnected = c;
                        bestUnconnected = u;
                    }
                }
            }
            
            // Add the connection and update sets
            if (bestConnected != null && bestUnconnected != null)
            {
                connections.Add((bestConnected, bestUnconnected));
                connected.Add(bestUnconnected);
                unconnected.Remove(bestUnconnected);
            }
        }
        
        return connections;
    }
    
    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int end, HashSet<Vector3Int> validTiles)
    {
        // A* pathfinding
        var openSet = new List<Vector3Int> { start };
        var closedSet = new HashSet<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int>();
        var fScore = new Dictionary<Vector3Int, int>();
        
        gScore[start] = 0;
        fScore[start] = ManhattanDistance(start, end);
        
        while (openSet.Count > 0)
        {
            // Get node with lowest fScore
            Vector3Int current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i]] < fScore[current])
                {
                    current = openSet[i];
                }
            }
            
            // If we reached the goal
            if (current.Equals(end))
            {
                return ReconstructPath(cameFrom, current);
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            // Check all neighbors
            foreach (var neighbor in GetNeighbors(current, validTiles))
            {
                if (closedSet.Contains(neighbor))
                    continue;
                
                int tentativeGScore = gScore[current] + 1;
                
                // Add turn penalty
                if (cameFrom.ContainsKey(current))
                {
                    Vector3Int prev = cameFrom[current];
                    Vector3Int currentDir = current - prev;
                    Vector3Int nextDir = neighbor - current;
                    
                    if (currentDir != nextDir)
                    {
                        tentativeGScore += turnCostMultiplier;
                    }
                }
                
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, int.MaxValue))
                {
                    continue; // Not a better path
                }
                
                // This path is the best so far
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + ManhattanDistance(neighbor, end);
            }
        }
        
        // No path found
        return new List<Vector3Int>();
    }
    
    private List<Vector3Int> GetNeighbors(Vector3Int pos, HashSet<Vector3Int> validTiles)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        Vector3Int[] directions = {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, -1, 0)
        };
        
        foreach (var dir in directions)
        {
            Vector3Int neighbor = pos + dir;
            if (validTiles.Contains(neighbor) && !occupiedTiles.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }
        
        return neighbors;
    }
    
    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int> { current };
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }
    
    private void ApplyPaths(HashSet<Vector3Int> validTiles)
    {
        if (pathTile == null)
        {
            Debug.LogError("Path tile is not assigned!");
            return;
        }
        
        // Expand paths for width
        HashSet<Vector3Int> expandedPaths = new HashSet<Vector3Int>();
        
        foreach (Vector3Int pos in pathTiles)
        {
            expandedPaths.Add(pos);
            
            // Add extra tiles for path width
            if (pathWidth > 1)
            {
                for (int dx = -pathWidth + 1; dx < pathWidth; dx++)
                {
                    for (int dy = -pathWidth + 1; dy < pathWidth; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        Vector3Int offset = new Vector3Int(pos.x + dx, pos.y + dy, pos.z);
                        if (validTiles.Contains(offset) && !occupiedTiles.Contains(offset))
                        {
                            expandedPaths.Add(offset);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Applying {expandedPaths.Count} path tiles ('{pathTile.name}')");
        
        // Track success/failure counts for debugging
        int succeeded = 0;
        int failed = 0;
        
        // Apply path tiles via WorldHandler
        foreach (Vector3Int pos in expandedPaths)
        {
            if (occupiedTiles.Contains(pos)) continue;
            
            bool success = worldHandler.SetTile(pos, pathTile);
            
            if (success)
                succeeded++;
            else
                failed++;
        }
        
        Debug.Log($"Path application complete: {succeeded} succeeded, {failed} failed");
    }
    
    private int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    private Vector3Int CalculateMidpoint(List<Vector3Int> positions)
    {
        if (positions == null || positions.Count == 0)
            return Vector3Int.zero;
            
        int sumX = 0, sumY = 0, sumZ = 0;
        
        foreach (var pos in positions)
        {
            sumX += pos.x;
            sumY += pos.y;
            sumZ += pos.z;
        }
        
        return new Vector3Int(
            sumX / positions.Count,
            sumY / positions.Count, 
            sumZ / positions.Count
        );
    }
}