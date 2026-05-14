# Enemy State Documentation

This document describes the implementation of the Enemy in C#.

## Location

The core implementation is located in the `SubProjects/CSharpLibrary/Scripts/Game/Enemy/` directory.

## Components

The Enemy is composed of three main scripts:

### 1. `TestEnemy.cs`

This script handles the basic AI and movement of the enemy.

*   **States:**
    *   **Idle/Searching:** When the player is outside the `searchRange`, the enemy is in a searching state. Its color is green.
    *   **Chasing:** When the player is within the `searchRange` but outside the `attackRange`, the enemy will chase the player. Its color is khaki.
    *   **Attacking:** When the player is within the `attackRange`, the enemy is in an attacking state. Its color is red.
*   **Serialized Fields:**
    *   `ENTITY_NAME`: The name of the entity to target (default: "Player").
    *   `searchRange`: The range at which the enemy starts to detect the player.
    *   `rotateSpeed`: The speed at which the enemy rotates to face the player.
    *   `attackRange`: The range at which the enemy starts to attack the player.
    *   `speed`: The movement speed of the enemy.

### 2. `EnemyCollisionHandler.cs`

This script manages the enemy's health, damage, and collision.

*   **States:**
    *   **Alive:** The enemy is alive and can take damage.
    *   **Damaged:** The enemy has been hit and is temporarily invulnerable (`damageCooldown`).
    *   **Destroyed:** When `hitpoints` are less than or equal to 0, the `isDestroy` flag is set to true, and the entity is destroyed in the next `Update` cycle.
*   **Serialized Fields:**
    *   `MAX_HITPOINTS`: The maximum health of the enemy.
    *   `DAMAGE_COOLDOWN_TIME`: The duration of invulnerability after taking damage.

### 3. `EnemyUIHandler.cs`

This script controls the enemy's UI, specifically the health bar.

*   **Functionality:**
    *   It finds a child entity named "HP" and gets its `DissolveMeshRenderer` component.
    *   The `OnDamaged` method is called by `EnemyCollisionHandler` to update the `threshold` of the `DissolveMeshRenderer`, which visually represents the enemy's current health.
*   **Serialized Fields:**
    *   `HP_UI_NAME`: The name of the child entity that represents the HP bar (default: "HP").
