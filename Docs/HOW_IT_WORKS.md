# How WICKFALL works — the short version

For each topic: **what it is**, **where it is**, and **what to say to the teacher**.

---

### 1. Unity basics
Everything in the game is a **GameObject** with **components** (scripts) on it. Unity calls `Update()` every frame and `FixedUpdate()` for physics.

> 🗣️ "In Unity every object is made of components. My scripts inherit from `MonoBehaviour`. Unity calls `Update` every frame and `FixedUpdate` for physics."

### 2. Movement
Keys → a direction → limited to 4 directions → speed → the physics body moves the player.
📁 `Assets/Scripts/Runtime/Player/PlayerController.cs` (look for `Move` and `FixedUpdate`)

> 🗣️ "The Input System gives a direction. I limit it to 4 directions like the PDF says. I turn it into a speed, and the `Rigidbody2D` moves the player. Walls stop him because of colliders."

### 3. Camera
The camera follows the player smoothly and looks a bit ahead of where you walk.
📁 `Assets/Scripts/Runtime/World/CameraRig.cs` (look for `LateUpdate` and `SmoothDamp`)

> 🗣️ "The camera follows the player in `LateUpdate`, after he has moved. `SmoothDamp` makes it glide like a spring. A Pixel Perfect Camera keeps the pixels sharp."

### 4. Wax and light (the main idea of the game)
Wax goes down over time. Less wax → smaller light. More light → enemies see you from farther.
📁 `Assets/Scripts/Runtime/Logic/LightMath.cs`

> 🗣️ "Wax drops by 0.5 per second. The light radius goes from 1.2 to 6 depending on wax. Enemies detect you at 2 + radius × 1.5. Shining more lets you see more, but you are seen more too."

### 5. Enemies
Each enemy has a **state machine**: wander → alert → chase → attack. It sees you if you are inside its detection range and no wall is in the way.
📁 `Assets/Scripts/Runtime/Enemies/EnemyBrain.cs`

> 🗣️ "Each enemy has a state machine. It is in only one state at a time, and it switches state depending on what it sees."

### 6. Levels
Levels are drawn as text maps by small scripts, then Unity turns them into tiles automatically. Only the rooms near the player are loaded.
📁 `Tools/levels/` and `Assets/Scripts/Runtime/World/RoomLoader.cs`

> 🗣️ "Levels are generated from text maps. Only the rooms near the player are loaded, to save memory."

### 7. Tests
Bots play every region and reach all three endings. Other tests check every number of the PDF.
📁 `Assets/Tests/`

> 🗣️ "Automatic tests check the PDF's rules, and a bot finishes every region, so I know the game works."

---

**Try it:** open `Assets/Scenes/Boot.unity`, press **Play**, then press **`** to see wax, light and enemy states live.
