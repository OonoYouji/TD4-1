# Boss Implementation Tasks (v2)

Based on the updated `BossSpecification_v2.md`.

## Phase 1: Core Systems & HP Management
- [ ] **HP System Refinement**
    - [ ] Implement constant HP regeneration logic in `HP.cs` (configurable per phase).
    - [ ] Implement phase transition triggers based on HP thresholds.
- [ ] **Movement System**
    - [ ] Implement grid-based waypoint movement (sequential travel 1 -> 2 -> 3...).
    - [ ] Implement "Push/Bulldoze" logic: Boss maintains speed even when colliding with rocks.
    - [ ] Add movement parameters: `WaitTime`, `Speed`, `Waypoints`.

## Phase 2: Phase 1 Attacks (Introduction)
- [ ] **Attack 1: Targeted Beam**
    - [ ] Target logic: Find the cluster of reinforcements.
    - [ ] Implementation of prediction line (visual update during wait time).
    - [ ] Beam firing logic (fixed duration).
- [ ] **Attack 2: Clogging Attack (Enlargement)**
    - [ ] Area-of-Effect (AoE) logic: Detect reinforcements within radius.
    - [ ] Scaling logic: Temporarily increase the scale of reinforcements.
    - [ ] Balancing: Ensure the "clogging" effect causes physical congestion.
- [ ] **Attack 4: Rock Lift & Drop**
    - [ ] Target logic: Identify random rocks and reinforcement clusters.
    - [ ] Animation/State: Lifting, Aiming, and Dropping sequence.

## Phase 3: Phase 2+ Attacks (Full Arsenal)
- [ ] **Attack 3: Rotating Bomb Barrage**
    - [ ] Rotation logic: Boss rotates at specific speed.
    - [ ] Projectile logic: Parabolic trajectory with two alternating distances.
    - [ ] Visual: Prediction markers for landing spots.
- [ ] **Attack 5: Vortex/Pulling Fields**
    - [ ] Spawn logic: Multiple vortex points arranged in a circle.
    - [ ] Physics: Apply pulling force to reinforcements, overriding their original velocity.
- [ ] **Attack 6: Sequential Pillar Drop**
    - [ ] Targeting: Detect player direction relative to Boss.
    - [ ] Spawning: Spawn pillars one by one in an arc/sequence.

## Phase 4: Integration & AI
- [ ] **Behavior Tree Integration**
    - [ ] Create BT nodes for each new attack pattern.
    - [ ] Implement "Movement -> Attack" sequence logic.
    - [ ] Handle concurrent execution of movement and actions (if required by "multiple actions" spec).
- [ ] **Visuals & Effects**
    - [ ] Update Boss model/animations to match v2 actions.
    - [ ] Add particle effects for Beam, Fire/Explosion, and Vortex.

## Phase 5: Testing & Tuning
- [ ] **Balance HP Regen vs Player DPS.**
- [ ] **Verify "Clogging" effect isn't too frustrating or game-breaking.**
- [ ] **Confirm Waypoint movement sequence works without getting stuck.**
