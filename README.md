# Unraveling

A 3D psychological horror game built in Unity exploring social anxiety and sensory overload in a classroom setting.

## Installation

1. **Clone the repository**
   ```bash
   git clone [https://github.com/your-username/unraveling.git](https://github.com/your-username/unraveling.git)


## Open in Unity

Open Unity Hub and add the project directory.

Launch using Unity 6000.3.13f1.

Build Target: Set to PC, Mac & Linux Standalone (File > Build Settings).


## Usage
Controls
Mouse: Look around (seated, limited range).

A / D: Lean left/right to dodge incoming creatures.

E (Hold): Hold open textbook to intercept healing letters.

Left-Click: Close textbook to capture letters (subject to cooldown).


## Core Mechanics
Teacher Gaze & Suspicion Ladder: Direct eye contact increases teacher suspicion via raycasting. Higher levels trigger erratic behaviors, unnatural animations, and immediate fail states.

Paranoia Level: Increased by eye contact and entity hits. Drives environmental distortion, camera shakes, atmospheric shifts, and fainting at maximum threshold.

Dodging & Healing: Lean away from attacking creature prefabs. Catch floating healing letters with textbook timing to lower paranoia.


## Coding Style
Naming: PascalCase for classes/methods/enums; camelCase for variables; _camelCase for private fields.



## Contribution Rules
Comment Code: Document non-obvious logic and state transitions.

Branching: Do not commit directly to main. Create feature branches (feature/your-feature).

PR Approvals: Have a sublead code review before merging into main.
