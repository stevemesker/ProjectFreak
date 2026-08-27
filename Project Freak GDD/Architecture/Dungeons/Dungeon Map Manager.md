## Overview

`DungeonMapManager` is responsible for generating and initializing the dungeon's navigational map.

The manager takes a `DungeonSO` containing the configuration for the current dungeon and uses that data to:

1. Generate the floor node grid.
2. Position the nodes within the map's UI floor zone.
3. Create the initial vertical connections between nodes in each column.
4. Create the entrance and boss nodes.
5. Connect the entrance to the first row of floor nodes.
6. Connect the boss node to the final row of floor nodes.
7. Detect additional nearby nodes that can be connected.
8. Create and position the visual bridge objects representing node connections.

The manager primarily handles **map construction and orchestration**. Individual nodes are responsible for maintaining their own connection data and determining which nearby nodes they can connect to.

---

## Responsibilities

### Map Initialization

`StartNewMap()` begins generation of a new dungeon map.

It:

* Stores the supplied `DungeonSO` as the current dungeon data.
* Validates that all required map zones and bridge references have been assigned.
* Generates the standard floor nodes.
* Generates the entrance and boss nodes.
* Begins the node connection process.

### Floor Node Generation

`SpawnFloorNodes()` creates the main grid of dungeon map nodes.

The number of nodes is determined by:

* `_DungeonColumnCount`
* `_DungeonRowCount`

from the active `DungeonSO`.

Each node receives:

* A unique ID.
* A column number.
* A calculated UI position.

The node's position is affected by the dungeon's node distribution curve and configured map wiggle value, allowing the otherwise grid-based map to have a more organic appearance.

### Key Node Generation

`SpawnKeyNodes()` creates the two special nodes that bookend the dungeon map:

* **Entrance**
* **Boss**

The entrance is connected to every node in the first row.

The boss is connected to every node in the final row.

Both nodes use the same floor node prefab as normal nodes, but their `DungeonMapNode._Type` value identifies their special role.

### Initial Connections

When floor nodes are spawned, nodes within the same column are immediately connected vertically.

This creates the basic structure of the map before additional connections are calculated.

The manager also creates a `NodeBridge` object for each initial connection.

### Connection Detection

After node generation, `connectNodes()` asks each node to determine which other nodes are within its valid connection range.

The actual connection rules are handled by [[Dungeon Map Node]]. The manager simply coordinates the process.

The detection is intentionally delayed by one frame through `detectNodeRange()`.

This allows the generated map objects to finish initialization before connection detection begins.

### Floor Node Type Pool

Each dungeon path maintains its own runtime **Floor Node Type Pool**, populated from the path's designer-defined `FloorPoolEntry` data.

- The configured dungeon floor pool size determines the target number of entries in the pool.
- Each `FloorPoolEntry` specifies a `POIType` and a percentage-based weight.
- The generator converts each percentage into a number of entries and adds that many copies of the corresponding `POIType` to the pool.
- Each entry is guaranteed to contribute at least **1 card**, even if its calculated percentage would produce less than 1.
- If the resulting pool does not reach the configured minimum pool size, the remaining entries are filled with the default `Basic` POI type.
- When assigning a type to a dungeon node, the system selects a random entry from the pool and **removes it from the pool**.
- This creates a **weighted-bag/deck system** rather than independent random rolls: once a type has been drawn, it cannot be drawn again until that path's pool is exhausted.
- When the pool becomes empty, it is automatically regenerated from the path's configured weights and the process repeats.

This system provides controlled randomness while maintaining the intended overall distribution of node types. Designers can adjust the percentages and pool size during balancing without changing the underlying generation logic.

---

# Inspector Data

## Data

### `_CurrentDungeonData`

**Type:** `DungeonSO`

The dungeon configuration currently being used to generate the map.

This provides the map generation parameters, including:

* Number of columns.
* Number of rows.
* Node position wiggle.
* Other dungeon-specific generation data.

This value is assigned when `StartNewMap()` is called.

### `_nodeDistribution`

**Type:** `AnimationCurve`

Controls how node position offsets are distributed across the columns of the dungeon map.

The curve is evaluated by `curveEval()` using the node's column position.

This allows the map to have controlled horizontal/vertical variation rather than applying completely uniform random offsets.

---

# Map Zone References

## `_BossZone`

The UI zone used as the parent and spawn location for the boss node.

## `_FloorZone`

The main UI region containing the generated floor nodes.

Its `RectTransform` dimensions are used when calculating the positions of generated nodes.

## `_EntranceZone`

The UI zone used as the parent and spawn location for the entrance node.

## `_BridgeZone`

The UI zone used as the parent for generated `NodeBridge` objects.

---

# Runtime Data

## `_FloorNodes`

**Type:** `List<GameObject>`

Contains every generated map node.

Despite the name, this list also contains the entrance and boss nodes after `SpawnKeyNodes()` has executed.

The list is therefore the manager's complete collection of generated map nodes.

Nodes are added in generation order:

1. Standard floor nodes.
2. Entrance node.
3. Boss node.

The ordering of the standard floor nodes is important because several generation operations use the node index to determine its row and column.

## `_EntranceNode`

**Type:** `DungeonMapNode`

Direct reference to the generated entrance node.

This is assigned during `SpawnKeyNodes()`.

---

# Prefab Settings

## `_floorNodePrefab`

Prefab used when creating all map nodes.

The prefab is expected to contain a `DungeonMapNode` component and the interfaces/components required for node bridging.

The same prefab is used for:

* Normal floor nodes.
* Entrance node.
* Boss node.

The node's `_Type` determines the role of the generated node.

## `_lineConnectionPrefab`

Prefab used to visually represent connections between map nodes.

The instantiated object is expected to contain a `NodeBridge` component.

---

# Generation Flow

The complete generation sequence is:

```text
StartNewMap()
    |
    +-- Validate required references
    |
    +-- SpawnFloorNodes()
    |       |
    |       +-- Create floor node grid
    |       +-- Assign IDs
    |       +-- Assign column numbers
    |       +-- Position nodes
    |       +-- Create vertical connections
    |       +-- Create NodeBridge objects
    |       +-- SpawnKeyNodes()
    |               |
    |               +-- Create Entrance
    |               +-- Create Boss
    |               +-- Connect Entrance to first column
    |               +-- Connect Boss to final column
    |
    +-- connectNodes()
    |
    +-- detectNodeRange()
            |
            +-- Wait one frame
            +-- connectNodes()
```

---

# Node Positioning

Floor nodes are positioned relative to the dimensions of `_FloorZone`.

The base position is calculated from the number of rows and columns:

```text
X = Floor Width / (Column Count + 1) * Column Position
Y = Floor Height / (Row Count - 1) * Row Position
```

A procedural offset is then added using `curveEval()`.

The resulting position provides a mostly grid-based layout while allowing the map to visually bend and shift between columns.

## `curveEval()`

`curveEval()` generates a random positional offset for a column.

The direction of the offset is randomized between positive and negative values, while the magnitude is controlled by:

* `_DungeonMapNodeWiggle`
* `_nodeDistribution`

This produces variation in the map while keeping the overall structure controlled by the designer.

---

# Connection Architecture

The map uses two separate concepts for connections:

### Logical Connection

A `DungeonMapNode` stores another node in its `_NodeConnections` collection.

This represents the actual navigational relationship between two map nodes.

### Visual Connection

A `NodeBridge` represents the visual line between two nodes.

When a connection is created, both nodes are informed of the bridge through `IBridgeable.BridgeNode()`.

The bridge itself is then configured through:

```text
NodeBridge.BuildConnection()
NodeBridge.updatePosition()
```

This separation allows the map's logical navigation data and its visual representation to remain distinct.

---

# Initial Connection Structure

The initial map generation creates guaranteed connections before range-based connection detection occurs.

For each column:

```text
Node
  |
Node
  |
Node
  |
Node
```

Every node in a column is connected to the node immediately preceding it.

This ensures that each column has a basic vertical path structure.

The entrance and boss nodes then extend this structure:

```text
        Entrance
        /   |   \
       /    |    \
     Row 1 Row 1 Row 1
       |    |    |
     Row 2 Row 2 Row 2
       |    |    |
     Row 3 Row 3 Row 3
       |    |    |
       \    |    /
        \   |   /
          Boss
```

Additional connections can then be created based on node proximity.

---

# Connection Detection

## `connectNodes()`

Iterates through every generated node and checks whether it is capable of creating bridges.

Nodes that return `false` from `IBridgeable.canBridge()` are skipped.

For nodes that can create connections:

```csharp
DungeonMapNode.ConnectNodesInRange();
```

is called.

The actual range calculation and connection rules belong to `DungeonMapNode`.

This keeps the manager focused on **when** connection detection occurs rather than **how** a node determines its valid connections.

---

# Delayed Detection

## `detectNodeRange()`

`detectNodeRange()` is a coroutine used to delay the final connection detection by one frame.

```csharp
IEnumerator detectNodeRange()
{
    yield return null;
    connectNodes();
}
```

The delay gives Unity an opportunity to complete the initialization and positioning of all generated map objects before the nodes perform their range checks.

This is particularly important because the connection system relies on the generated nodes having their final world/UI positions.

---

# Test Tools

The manager contains Odin Inspector buttons for manually testing map generation.

## `test()`

Clears the existing map and generates a new floor-node layout.

The current implementation intentionally omits `connectNodes()` from the immediate test sequence and instead relies on `detectNodeRange()` to perform the delayed connection detection.

This provides a convenient way to repeatedly test map generation directly from the Unity Inspector.

## `clear()`

Destroys all currently generated nodes and clears `_FloorNodes`.

It also resets `_EntranceNode`.

This method uses `DestroyImmediate()` because it is intended primarily as an editor/testing utility.

---

# Dependencies

`DungeonMapManager` relies on the following systems/components:

### `DungeonSO`

Provides the configuration used to generate the map.

### `DungeonMapNode`

Represents an individual node on the dungeon map and manages its logical connections.

### `IBridgeable`

Provides the interface used by the manager to interact with objects that can participate in node bridges.

The manager uses:

* `ConnectNode()`
* `BridgeNode()`
* `canBridge()`

### `NodeBridge`

Represents the visual connection between two map nodes.

Responsible for constructing and positioning the visual bridge.

### `_floorNodePrefab`

Prefab containing the components required for a generated dungeon map node.

### `_lineConnectionPrefab`

Prefab containing the components required for a generated node bridge.

---

# Architectural Role

`DungeonMapManager` acts as the **map generation coordinator**.

It does not own the detailed behavior of individual nodes. Instead, responsibilities are divided as follows:

| System              | Responsibility                                             |
| ------------------- | ---------------------------------------------------------- |
| `DungeonMapManager` | Generate and initialize the overall map                    |
| `DungeonMapNode`    | Represent a node and manage its connections                |
| `IBridgeable`       | Provide bridge-related node interaction                    |
| `NodeBridge`        | Represent and visually position a connection               |
| `DungeonSO`         | Provide dungeon/map generation configuration               |
| Floor Node Prefab   | Define the visual/component structure of a map node        |
| Bridge Prefab       | Define the visual/component structure of a node connection |

This separation allows the map manager to remain focused on **generation and orchestration**, while individual map objects handle their own behavior.

---

# Future Extension Points

The manager currently contains an empty `setNodeType()` method.

This is the intended location for assigning gameplay types to generated nodes once map generation determines which locations should contain specific POIs.

Potential future responsibilities include:

* Assigning combat nodes.
* Assigning treasure nodes.
* Assigning rest nodes.
* Assigning event nodes.
* Assigning special POIs.
* Controlling node rarity/distribution.
* Ensuring required POI types appear in the generated map.

The generation pipeline can therefore eventually become:

```text
Generate Layout
      ↓
Create Connections
      ↓
Assign Node Types
      ↓
Validate Map
      ↓
Present Map
```

This keeps **physical map generation** separate from **gameplay/content assignment**, which should make the dungeon system easier to expand as more POI types are introduced.
