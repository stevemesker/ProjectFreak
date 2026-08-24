## Overview

`DungeonMapNode` represents an individual floor within the dungeon map.

Each node stores its identity, dungeon position, gameplay type, connected nodes, and visual bridge relationships. It is also responsible for determining which nearby nodes it can connect to during map generation.

The node implements `IBridgeable`, allowing [[Dungeon Map Manager]] and other map nodes to interact with it through a common bridge/connection interface.

The manager is responsible for **creating and positioning nodes**, while [[Dungeon Map Node]] is responsible for **managing its own connections**.

---

# Responsibilities

`DungeonMapNode` is responsible for:

* Storing node identity and map position data.
* Tracking the node's column.
* Storing the node's gameplay/POI type.
* Tracking logical node connections.
* Tracking visual bridge objects.
* Determining its connection detection range.
* Searching for nearby nodes.
* Prioritizing nearby nodes by distance.
* Creating additional connections up to its connection limit.
* Providing bridge-related functionality through `IBridgeable`.

---

# Data

## `_ID`

**Type:** `int`

Unique identifier assigned to the node when the dungeon map is generated.

The ID is currently assigned sequentially by `DungeonMapManager`.

The manager uses the order in which nodes are generated to determine their IDs.

---

## `_ColumnNumber`

**Type:** `int`

Stores the node's column within the dungeon map.

This allows systems working with the map to determine the node's progression through the dungeon without needing to infer its position from its world coordinates.

Normal floor nodes are assigned their column during map generation.

The entrance and boss nodes are assigned a column value beyond the normal dungeon columns.

---

## `_Type`

**Type:** `POIType.Type`

Defines the gameplay role of the node.

Examples include:

* Entrance
* Boss
* Other POI types defined by `POIType`

Normal floor nodes can have their type assigned later in the generation process.

The node itself does not currently determine its own type.

---

## `_NodeConnections`

**Type:** `List<GameObject>`

Contains references to all map nodes that are logically connected to this node.

A connection is stored from both sides.

For example:

```text id="6n3h1p"
Node A ←→ Node B
```

Both nodes contain the other node in their `_NodeConnections` list.

This allows a node to independently determine which other nodes it can travel to.

The list is also used to enforce `_ConnectionsMax`.

---

## `_BridgeConnections`

**Type:** `Dictionary<GameObject, GameObject>`

Maps connected nodes to the `NodeBridge` object representing their visual connection.

The structure is:

```text id="r0zv9d"
Connected Node → NodeBridge
```

For example:

```text id="5fx8nq"
Node B → Bridge AB
Node C → Bridge AC
Node D → Bridge AD
```

This provides a direct way to retrieve the visual bridge associated with a particular logical connection.

The dictionary is initialized in `Awake()`.

---

## `_FloorSceneName`

**Type:** `string`

Stores the scene associated with this dungeon map node.

This allows the node to identify the gameplay scene that should be loaded when the player selects or enters the node.

The field is currently data storage only; scene loading is handled elsewhere.

---

# Settings

## `_DetectionRange`

**Type:** `float`

Defines the maximum distance from this node in which other nodes can be considered for automatic connections.

The value can be established using `SetInitialDetectionRange()`.

During generation, the initial value is based on the distance to the node's existing connections.

---

## `_MinimumNodePlacementRange`

**Type:** `float`

Defines the minimum desired distance between nodes.

This value is currently stored by the node but is not actively used by the connection-generation logic.

It can potentially be used later to prevent nodes from being placed too close together or to influence connection validation.

---

## `_ConnectionsMax`

**Type:** `int`

Maximum number of logical connections the node can have.

The default value is `3`.

This value is enforced by `canBridge()` and `ConnectNodesInRange()`.

Once the node reaches the maximum number of connections, it will no longer attempt to create additional bridges.

---

# Initialization

## `Awake()`

Initializes the `_BridgeConnections` dictionary.

```csharp
_BridgeConnections = new Dictionary<GameObject, GameObject>();
```

The logical connection list is expected to be populated during dungeon map generation.

---

# Detection Range

## `SetInitialDetectionRange()`

Determines the node's initial connection detection range based on its existing connections.

The method:

1. Takes the first existing connection as the initial distance.
2. Calculates the distance to every existing connection.
3. Keeps the greatest distance found.
4. Stores that distance as `_DetectionRange`.

Conceptually:

```text id="6m4j2a"
Detection Range =
    Maximum distance between this node
    and any of its existing connections
```

This allows the initial guaranteed connections created by `DungeonMapManager` to establish a useful search radius for finding additional nearby nodes.

### Important Assumption

The method assumes that `_NodeConnections` contains at least one node.

Calling this method before the node has an initial connection would result in an index error when accessing:

```csharp
_NodeConnections[0]
```

---

# Automatic Connection Generation

## `ConnectNodesInRange()`

Searches for nearby nodes and attempts to establish additional connections.

This is the primary connection-generation method on the node.

### Process

The method performs the following steps:

```text id="uw3w2a"
Find all colliders within Detection Range
            ↓
Sort results by distance
            ↓
Ignore existing connections
            ↓
Ignore itself
            ↓
Check connection limit
            ↓
Check whether target can bridge
            ↓
Create logical connection
            ↓
Create visual bridge
```

---

## Finding Nearby Nodes

The method uses:

```csharp
Physics2D.OverlapCircleAll(
    transform.position,
    _DetectionRange
);
```

This finds all `Collider2D` objects within the node's detection radius.

The node therefore relies on the map node prefabs having appropriate 2D colliders for detection.

---

# Connection Prioritization

After detecting nearby objects, the results are sorted by distance from the current node.

The comparison uses squared distance:

```csharp
(transform.position - a.transform.position).sqrMagnitude
```

rather than calculating the actual distance.

This provides the same ordering while avoiding unnecessary square-root calculations.

The result is that nodes are considered from nearest to farthest.

For example:

```text id="y6f6k2"
Current Node

    A       ← checked first
       B    ← checked second

          C ← checked third

                D ← checked fourth
```

This makes the node prefer nearby connections when its `_ConnectionsMax` limit is reached.

---

# Connection Validation

Each detected object is evaluated against several conditions.

## Already Connected

```csharp
_NodeConnections.Contains(hit.gameObject)
```

If the node is already connected to the target, it is skipped.

This prevents duplicate logical connections.

---

## Self-Connection

```csharp
hit.gameObject == gameObject
```

A node cannot connect to itself.

---

## Connection Limit

```csharp
_NodeConnections.Count >= _ConnectionsMax
```

Once the node has reached its maximum number of connections, the search stops.

Because detected objects are sorted by distance, this means the node keeps its closest valid connections.

---

## Target Bridge Availability

The target node's `IBridgeable.canBridge()` method is checked before creating the connection.

This prevents a node that has already reached its own connection limit from accepting another connection.

---

# Creating a Connection

Once a target node passes validation, the connection is created on both sides.

The current node adds the target:

```csharp
_NodeConnections.Add(hit.gameObject);
```

The target then adds the current node:

```csharp
hit.GetComponent<IBridgeable>().ConnectNode(gameObject);
```

This creates a bidirectional logical relationship:

```text id="j0t6tw"
Node A
  │
  └── Node B

Node A → contains Node B
Node B → contains Node A
```

---

# Creating the Visual Bridge

After the logical connection is established, a bridge prefab is instantiated.

The bridge is parented to the dungeon map manager's `_BridgeZone`.

The bridge is then registered with both nodes through:

```csharp
BridgeNode()
```

Finally, `NodeBridge` is instructed to construct and size the visual connection.

The sequence is:

```text id="x9h4kp"
Instantiate Bridge
        ↓
Set Bridge Pivot
        ↓
Register Bridge with Target Node
        ↓
BuildConnection()
        ↓
updatePosition()
```

The bridge therefore serves as the visual representation of the logical connection stored in `_NodeConnections`.

---

# IBridgeable Implementation

`DungeonMapNode` implements `IBridgeable` to provide a common interface for node connection operations.

## `BridgeNode()`

Registers a bridge associated with another node.

```csharp
_BridgeConnections.Add(origin, bridge);
```

The supplied node becomes the dictionary key, while the bridge object becomes the value.

This creates a lookup relationship:

```text id="4u6h8z"
Other Node → Bridge
```

---

## `canBridge()`

Determines whether the node can accept another connection.

Returns `false` when:

```csharp
_NodeConnections.Count >= _ConnectionsMax
```

Otherwise returns `true`.

This method is used by other nodes before attempting to connect to this node.

---

## `ConnectNode()`

Adds another node to `_NodeConnections`.

This method is used when another node creates a connection to this node.

The method does not perform validation itself; validation is expected to occur before it is called.

---

## `disconnectNodes()`

Currently not implemented.

```csharp
throw new System.NotImplementedException();
```

This is a future extension point for removing an existing node connection and its associated bridge.

A complete implementation will likely need to:

1. Remove the target from `_NodeConnections`.
2. Find the corresponding bridge in `_BridgeConnections`.
3. Remove the bridge from the dictionary.
4. Destroy or otherwise deactivate the visual bridge.

---

## `getMaxRange()`

Returns the node's current `_DetectionRange`.

This allows other systems using `IBridgeable` to query the node's connection range without directly accessing the field.

---

## `LoadReconnect()`

Currently not implemented.

This appears intended for reconstructing node connections when loading an existing dungeon map from saved data.

A future implementation could use saved node IDs to restore:

* Logical node connections.
* Bridge connections.
* Other map state.

---

# Debug Visualization

## `OnDrawGizmosSelected()`

Draws a wire sphere representing `_DetectionRange` when the node is selected in the Unity Editor.

This provides a visual representation of the node's current connection search radius.

It is useful for debugging:

* Node spacing.
* Connection ranges.
* Unexpected connections.
* Missing connections.

---

# Connection Architecture

The node maintains two related but separate representations of its connections.

### Logical Connections

Stored in:

```text id="f9f5w3"
_NodeConnections
```

These determine which nodes are connected for gameplay/navigation purposes.

### Visual Connections

Stored in:

```text id="n8w4ha"
_BridgeConnections
```

These map each connected node to the `NodeBridge` that visually represents that relationship.

This separation allows the gameplay relationship to exist independently from its visual representation.

---

# Example

A node with three connections would contain:

```text id="p3w6k1"
_NodeConnections

    ├── Node 12
    ├── Node 18
    └── Node 24
```

and:

```text id="e1j5c8"
_BridgeConnections

    Node 12 → Bridge 12-Current
    Node 18 → Bridge 18-Current
    Node 24 → Bridge 24-Current
```

The two collections describe the same relationships from different perspectives.

---

# Architectural Role

`DungeonMapNode` is the **individual connection-aware unit of the dungeon map**.

Its responsibilities can be summarized as:

```text id="w2v8q0"
DungeonMapManager
       │
       │ creates/configures
       ▼
DungeonMapNode
       │
       ├── Stores node data
       │
       ├── Stores logical connections
       │
       ├── Detects nearby nodes
       │
       ├── Determines valid connections
       │
       └── Creates NodeBridge representations
                    │
                    ▼
               NodeBridge
```

The division of responsibilities is intentionally:

| System              | Responsibility                                     |
| ------------------- | -------------------------------------------------- |
| `DungeonMapManager` | Generates and coordinates the complete map         |
| `DungeonMapNode`    | Manages an individual node and its connections     |
| `IBridgeable`       | Defines the connection/bridge interface            |
| `NodeBridge`        | Provides the visual representation of a connection |
| `DungeonSO`         | Provides dungeon generation configuration          |
| `POIType`           | Defines the gameplay role of a node                |

This architecture allows the map manager to remain relatively high-level while individual nodes handle the details of determining and maintaining their own connectivity.

---

# Current Limitations / Future Work

Several methods and settings currently provide extension points for the dungeon map system.

### Connection Removal

`disconnectNodes()` is not implemented.

This will be required if connections can be modified after generation or reconstructed from saved data.

### Save/Load Reconstruction

`LoadReconnect()` is not implemented.

A future implementation will likely need to reconstruct the map's logical and visual connections from persistent dungeon data.

### Minimum Placement Range

`_MinimumNodePlacementRange` is currently unused.

It can eventually be incorporated into map validation or node placement to prevent undesirable node clustering.

### Connection Path Validation

`ConnectNodesInRange()` currently considers nearby nodes based primarily on distance.

The existing code contains a note regarding checking whether another node is physically "in the way" of a connection.

A future implementation could validate connections against intervening nodes or other map constraints before creating the bridge.

### Manager Dependency

`ConnectNodesInRange()` accesses the `DungeonMapManager` through:

```csharp
transform.root.gameObject.GetComponent<DungeonMapManager>()
```

This means generated nodes currently assume that their root object contains the map manager.

If the map system is later reused in a different hierarchy, this dependency may need to be replaced with an explicit manager reference or another dependency-injection approach.
