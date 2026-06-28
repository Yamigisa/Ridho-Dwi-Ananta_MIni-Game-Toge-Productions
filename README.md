# Turn-Based RPG Prototype

A 2D narrative turn-based RPG developed in Unity as a programmer pre-interview
project for Toge Productions.

The prototype combines world exploration, NPC interaction, branching dialogue,
in-game cutscenes, party progression, quests, inventory management, and a
JRPG-style battle system. The project focuses on responsive game feel,
data-driven content, readable code, and clear separation between gameplay
systems.

## Submission Links

| Deliverable       | Link                                                                                         |
| ----------------- | -------------------------------------------------------------------------------------------- |
| Source repository | [GitHub repository](https://github.com/Yamigisa/Ridho-Dwi-Ananta_MIni-Game-Toge-Productions) |
| Gameplay video    | **Add public gameplay video link here**                                                      |

> Replace the two placeholders above before submitting the project.

## Features

### World Exploration

- 2D tile-based world with player, NPC, enemy, and interior environments
- Keyboard and gamepad movement through Unity's Input System
- Sprinting and responsive movement handling
- Context-based interaction with NPCs, objects, items, and scene entrances
- Enemy AI with configurable wandering, detection, and chasing behaviour
- Dialogue and cutscene input locks that pause player and enemy movement
- Scene transitions between the world, interiors, and battles

### Dialogue and Cutscenes

- Fungus-powered dialogue, character speech, and menu choices
- Reusable dialogue presentation across gameplay, battle, and interior scenes
- Unity Timeline sequences with automatic character movement
- Player control is disabled while dialogue or cutscenes are active
- Cutscene progress tracking prevents completed sequences from replaying
- Save transactions protect progression when a cutscene is interrupted

### Turn-Based Battle

- JRPG-style party-versus-enemy combat
- Speed-based visual turn order
- Attack, Skill, Defend, Item, Pass, and Flee actions
- Target selection and action previews
- Configurable skills with MP costs, targeting rules, damage, recovery, buffs,
  debuffs, and recurring effects
- Data-driven enemy AI capable of attacking, using skills or items, defending,
  passing, and fleeing
- Victory, defeat, successful escape, rewards, item drops, EXP, and leveling
- Persistent party composition and character progression between scenes

### Quests, Items, and Persistence

- ScriptableObject-driven quests with item collection and monster defeat goals
- Quest progression linked to Fungus blocks and Timeline cutscenes
- Inventory with collectible, consumable, and quest items
- Item use on individual party members
- Persistent inventory, party, stats, quest progress, picked-up items, defeated
  encounters, and world positions
- PlayerPrefs-backed save data with runtime-state separation

### Presentation

- Character animation for exploration and battle actions
- Turn-order, party, inventory, quest, and battle interfaces
- Scene-specific music for menus, exploration, interiors, and battles
- Action, skill, item, UI, victory, and game-over sound effects
- Pause menu, settings, music volume, and SFX volume controls

## Controls

| Action        | Keyboard                   | Gamepad          |
| ------------- | -------------------------- | ---------------- |
| Move          | `WASD` or Arrow Keys       | Left Stick       |
| Interact      | `E`                        | North Button     |
| Sprint        | Left Shift                 | Left Stick Press |
| Inventory     | `I`                        | Right Shoulder   |
| Party         | `C`                        | Select           |
| Pause         | `Esc`                      | Start            |
| UI navigation | Mouse or `WASD`/Arrow Keys | Stick or D-pad   |

Battle actions and targets are selected through the on-screen interface.

## Technical Overview

| Category    | Technology                                      |
| ----------- | ----------------------------------------------- |
| Engine      | Unity 6.3 LTS (`6000.3.13f1`)                   |
| Language    | C#                                              |
| Input       | Unity Input System                              |
| Narrative   | Fungus                                          |
| Cutscenes   | Unity Timeline                                  |
| Camera      | Cinemachine                                     |
| Rendering   | Unity 2D and Universal Render Pipeline packages |
| Persistence | PlayerPrefs with serializable save models       |

## Architecture

The project uses a data-driven and component-oriented structure:

- **ScriptableObjects** define units, battle attributes, exploration
  attributes, enemy AI profiles, skills, items, and quests.
- **Managers** coordinate high-level systems such as battles, dialogue, quests,
  audio, timelines, scenes, and global game state.
- **Runtime unit state** keeps mutable HP, MP, stats, level, and EXP separate
  from immutable unit configuration assets.
- **Events and relays** reduce direct dependencies between battle, quest,
  inventory, UI, and world systems.
- **Prefabs** provide reusable characters, UI panels, encounters, interiors,
  items, and system objects.
- **Scene transition state** carries the party and encounter context safely
  between exploration and battle scenes.

## Project Structure

```text
Assets/
├── Scenes/                  # Initializer, menu, gameplay, interior, and battle
├── Prefabs/                 # Reusable gameplay, UI, character, and system objects
├── Scriptable Objects/      # Units, skills, items, quests, and audio data
├── Scripts/
│   ├── Gameplay/
│   │   ├── Battle/          # Battle flow, AI, HUD, targeting, and turn order
│   │   ├── Core/            # Interactions, interiors, and scene transitions
│   │   ├── Item/            # Inventory, world items, and item persistence
│   │   ├── Player/          # Input, movement control, and interaction
│   │   ├── Quest/           # Quest definitions, state, and objective tracking
│   │   ├── Skill/           # Skill definitions and effects
│   │   └── Unit/            # Exploration, battle, party, and save state
│   └── System/
│       ├── Audio/           # Music and SFX management
│       ├── Core/            # Initialization and global game management
│       ├── Data/            # Persistent and transactional save data
│       ├── Dialogue/        # Fungus integration and popup messages
│       └── Timeline/        # Cutscene playback and progression
├── Timeline/                # Timeline assets
└── External Packages/       # Fungus and bundled third-party dependencies
```

## Assignment Coverage

| Requirement                                | Implementation                                             |
| ------------------------------------------ | ---------------------------------------------------------- |
| Player, NPC, enemy, and background objects | Included                                                   |
| WASD / Arrow Key world movement            | Included                                                   |
| World interaction                          | Included through a dedicated interaction input             |
| Dialogue                                   | Implemented with Fungus                                    |
| Automatic player movement in cutscenes     | Implemented with Timeline and scripted unit movement       |
| JRPG-style turn-based battle               | Included                                                   |
| ScriptableObject usage                     | Units, skills, items, quests, AI, and attributes           |
| Prefab implementation                      | Used throughout gameplay, characters, UI, and environments |
| Fungus narrative integration               | Included                                                   |

## Author

**Ridho Dwi Ananta**

Developed for the Toge Productions programmer pre-interview test.

## Usage Notice

This project was created for recruitment evaluation. Third-party tools, fonts,
art, music, and other assets remain subject to their respective licenses and
ownership terms.
