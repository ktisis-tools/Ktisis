# Ktisis
[![discord](https://img.shields.io/discord/975894364020686878)](https://discord.gg/kUG3W8B8Ny)

This is my attempt at creating a scene editing tool that allows for actor and bone manipulation.

If you would like to reach out about this project, please feel free to join the [Discord](https://discord.gg/kUG3W8B8Ny) or message me at chirp#1337.

#### List of target features, ascending:
- [x] Skeleton overlay
	- [ ] Selection assistant
- [x] Bone manipulation via overlay
	- [ ] Translate bone children
	- [ ] Transform locking
- [ ] Actor list with dropdowns for bone selection
- [ ] Free camera movement within GPose
	- [ ] Disable unwanted camera movements (i.e. on selection)

#### Features to explore, not currently in scope:
- Inverse kinematics
- Timeline editor for rudimentary animation
- Creation of additional actors and editing
- Placement and rendering of 3D props in-game
- Export characters as fully rigged Blender models

## Disclaimers

Ktisis can currently be considered in an early 'Alpha' state with research and development still ongoing.
<br/>
Features will be rolled out slowly, and bugs or crashes may be present.

It is not intended to be a replacement for CMTool or Anamnesis.
<br/>
My focus is not on recreating the functionality of either, so it will likely best be used in conjunction.

## Acknowledgements

- Thanks to the developers from [Goat Corp](https://github.com/goatcorp) and [XIV Tools](https://github.com/XIV-Tools), whose existing work in this area provided excellent insight into internal memory and data structures.
- [@Fayti1703](https://github.com/Fayti1703) and [@hackmud-dtr](https://github.com/hackmud-dtr) for their helpful insights into reverse engineering and low-level code, and overall helpfulness in answering questions.
- [@BobTheBob9](https://github.com/BobTheBob9) for their helpful insights into reverse engineering and IDA.
- The other developers from [Bnuuy Central](https://github.com/Bnuuy-Central) who were excellent practical and moral support.

This plugin is based off my original work from [xiv-reversal-rs](https://github.com/ktisis-tools/xiv-reversal-rs).