**Procedural Dungeon Stage Generator**

The dungeon is organized into **Game Sections**, each composed of one or more **Zones**, and each Zone contains a series of **Stages**. Every Stage maps to its own Unity scene, and the generator builds these Stages on the fly with branching paths and configurable transitions.

---

## **1\. Stage Types**

1. **Combat Stage**

   * Randomly pick from the Zone’s predefined set of combat scenes.

   * Each scene describes a sequence of **wave sets**, where each set specifies:

     * A prefab enemy type and spawn count, **or**

     * An optional “wild” mode that draws random enemies from the Zone’s enemy pool.

   **Spawning**: use for all enemies and bosses:

        public BaseEnemy SpawnEnemy(

       GameObject \_enemyPrefab,

       Vector3 \_position,

       Quaternion \_rotation,

       float \_telegraphDuration \= 0f

   )

2. **Challenge Stage**

   * Present an optional gauntlet (time trial, puzzle, obstacle course, or even another combat).

   * If the player declines, skip it without penalty.

3. **Boss Stage**

   * Always included and defined explicitly in the Zone’s configuration.

   * Loads the unique boss arena scene for that Zone.

4. **Shop Stage**

   * Transition to the card‑shop interface; no additional mechanics needed.

5. **Healing Stage**

   * Safe haven for HP restoration; scene alone suffices.

6. **Mystery Stage**

   * Player cannot see its type in advance.

   * Randomly resolves to **Combat**, **Challenge**, **Shop**, or **Healing**, but never **Boss**.

---

## **2\. Zone Definitions**

Each Zone’s configuration asset includes:

* **Stage Pool**: a list of potential Combat and Challenge scenes to choose from at random.

* **Branching Factor**: maximum number of exits per Stage (1–N).

* **Overall Length**: total number of Stages before triggering the Zone’s Boss Stage.

* **Boss Stage**: the designated boss scene, always appended last.

These settings replace any global defaults: each Zone controls its own variety, difficulty weighting, and path complexity.

---

## **3\. Runtime Construction**

1. **Starting Stage**

   * Always include a designated “start” Stage corresponding to the player’s current scene.

2. **Branching Paths**

   * Iteratively spawn Stages up to **Overall Length**, choosing types from the Zone’s **Stage Pool** and inserting the **Mystery Stage** logic when required.

   * Enforce the Zone’s **Branching Factor**: each Stage may create multiple forward portals.

3. **Transition Spawners**

   * Scan for `TransitionSpawner` objects in the active scene.

   * If there aren’t enough spawners:

     * Instantiate new ones on the NavMesh within walking range of the player.

   * For multi‑exit Stages, align portals in a clear linear or fan‑out layout.

   * Transitions only allow forward progress—no backtracking.

---

## **4\. Zone Progression**

* After defeating the Boss Stage, load a neutral **Interzone Stage or Start Stage** scene as the gateway to the next Zone.

* Carry over player state (HP, unlocked cards, gold.) into the next Zone’s run.

---

With these definitions, each Zone becomes a self‑contained ecosystem of randomized Combat and Challenge Stages, dynamic Mystery Stages, and a climactic Boss Stage—crafted by simple data-driven assets and a robust runtime generator.

