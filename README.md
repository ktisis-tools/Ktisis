<img width="1920" height="576" alt="Ktisis Logo Horizontal White" src="https://github.com/user-attachments/assets/c2c171ef-8df3-40d0-a8eb-5204a2876097" />

# Ktisis
[![discord](https://img.shields.io/discord/975894364020686878)](https://discord.gg/kUG3W8B8Ny)

Ktisis is a robust posing tool for creating screenshots within FFXIV's GPose mode which allows you to pose characters by clicking on bones and moving them in any way you want, bringing additional features such as animation control and character editing. Our primary aim is to make posing more accessible for all.

Please feel free to join us on [Discord](https://discord.gg/kUG3W8B8Ny) to get in touch with developers and receive regular progress updates.

For documentation, tutorials, and info on everything Ktisis can do, visit our [docs hub](https://docs.ktisis.tools/)!

## Installation

Ktisis is written as a Dalamud plugin and as such, requires that you use [XIVLauncher](https://goatcorp.github.io/) to start your game.

1. Type the `/xlsettings` command into your chatbox. This will open your Dalamud Settings.
2. In the window that opens, navigate to the "Experimental" tab. Scroll down to "Custom Plugin Repositories".
3. Copy and paste the repo URL into the input box, **making sure to press the "+" button to add it.**
4. Press the "Save and Close" button. This will add Ktisis to Dalamud's list of available plugins.
5. Open the plugin installer by typing the `/xlplugins` command, search for Ktisis, and click install.

#### Repo URL
Available through the [Sea Of Stars](https://github.com/Ottermandias/SeaOfStars) plugin suite or using our dedicated URL.

`https://raw.githubusercontent.com/ktisis-tools/Ktisis/main/repo.json`

## Contributing

Contributions are generally welcome, as long as they adhere to the following principles:
- Manipulation of any ingame object must be confined within GPose.
- It must not automate any tasks that result in network packets being sent to the server.
- Any changes to the client state must not be:
  - a) Permanent or irreversible by the user.
  - b) Detectable by the server, directly or indirectly.

Ktisis makes heavy use of unsafe code. If you are inexperienced or unfamiliar with the risks of this, then refrain from making code contributions that depend on it. Pull requests that show a reckless disregard for memory safety may be closed without further review.

Pull requests containing new features must be reviewed by an organization member or repo maintainer before being merged. Follow the [Dalamud Code of Conduct](https://dalamud.dev/code-of-conduct) when submitting.

## Acknowledgements

Huge thanks go out to:
- [Goat Corp](https://github.com/goatcorp) and [XIV Tools](https://github.com/XIV-Tools), whose existing work in this area provided excellent insight into internal memory and data structures.
- [@BobTheBob9](https://github.com/BobTheBob9), [@Fayti1703](https://github.com/Fayti1703) and [@hackmud-dtr](https://github.com/hackmud-dtr) for their help with reverse engineering and low-level code.
- The other developers from [Bnuuy Central](https://github.com/Bnuuy-Central) who have supported and helped me through this project's development.
- perchird ([@lmcintyre](https://github.com/lmcintyre/)) for their amazing work on Havok structs which helped to make this possible.
- [@Emyka](https://github.com/Emyka) for their continued efforts to help with accessibility and customisation.
- Everyone who has contributed code and features through pull requests!
