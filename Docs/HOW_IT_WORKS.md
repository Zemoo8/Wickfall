# How WICKFALL works — the short version

For each topic: **what it is**, **where it is**, and **what to say to the teacher** (in French).

---

### 1. Unity basics
Everything in the game is a **GameObject** with **components** (scripts) on it. Unity calls `Update()` every frame and `FixedUpdate()` for physics.

> 🗣️ « Dans Unity, chaque objet a des composants. Mes scripts héritent de `MonoBehaviour`. Unity appelle `Update` à chaque image et `FixedUpdate` pour la physique. »

### 2. Movement
Keys → a direction → limited to 4 directions → speed → the physics body moves the player.
📁 `Assets/Scripts/Runtime/Player/PlayerController.cs` (look for `Move` and `FixedUpdate`)

> 🗣️ « Le *Input System* donne une direction. Je la limite à 4 directions comme dans le PDF. Je calcule une vitesse et le `Rigidbody2D` déplace le joueur. Les murs le bloquent grâce aux colliders. »

### 3. Camera
The camera follows the player smoothly and looks a bit ahead of where you walk.
📁 `Assets/Scripts/Runtime/World/CameraRig.cs` (look for `LateUpdate` and `SmoothDamp`)

> 🗣️ « La caméra suit le joueur dans `LateUpdate`, après son déplacement. `SmoothDamp` la fait glisser comme un ressort. Une *Pixel Perfect Camera* garde les pixels nets. »

### 4. Wax and light (the main idea of the game)
Wax goes down over time. Less wax → smaller light. More light → enemies see you from farther.
📁 `Assets/Scripts/Runtime/Logic/LightMath.cs`

> 🗣️ « La cire baisse de 0,5 par seconde. Le rayon de lumière va de 1,2 à 6 selon la cire. Les ennemis détectent à 2 + rayon × 1,5. Briller, c'est voir mais aussi être vu. »

### 5. Enemies
Each enemy has a **state machine**: wander → alert → chase → attack. It sees you if you are inside its detection range and no wall is in the way.
📁 `Assets/Scripts/Runtime/Enemies/EnemyBrain.cs`

> 🗣️ « Chaque ennemi a une machine à états. Il ne peut être que dans un état à la fois, et il change d'état selon ce qu'il voit. »

### 6. Levels
Levels are drawn as text maps by small scripts, then Unity turns them into tiles automatically. Only the rooms near the player are loaded.
📁 `Tools/levels/` and `Assets/Scripts/Runtime/World/RoomLoader.cs`

> 🗣️ « Les niveaux sont générés à partir de cartes texte. Seules les salles proches sont chargées, pour économiser la mémoire. »

### 7. Tests
Bots play every region and reach all three endings. Other tests check every number of the PDF.
📁 `Assets/Tests/`

> 🗣️ « Des tests automatiques vérifient les règles du PDF, et un bot finit chaque région, donc je sais que le jeu marche. »

---

**Try it:** open `Assets/Scenes/Boot.unity`, press **Play**, then press **`** to see wax, light and enemy states live.
