# Ktisis
[![discord](https://img.shields.io/discord/975894364020686878)](https://discord.gg/kUG3W8B8Ny)

Ktisis is a robust posing tool for creating screenshots within FFXIV's GPose mode which allows you to pose characters by clicking on bones and moving them in any way you want, bringing additional features such as animation control and character editing. Our primary aim is to make posing more accessible for all.

Please feel free to join us on [Discord](https://discord.gg/kUG3W8B8Ny) to get in touch with developers and receive regular progress updates.

To see a list of target features as well as their current progress on implementation, see our [Trello](https://trello.com/b/w64GYAWJ/ktisis-plugin) board.

## Disclaimers

Ktisis is still in early development, and any releases that go out will be primarily for alpha testing. This means that some functionality may be buggy or broken, so it's recommended that you save your poses regularly and report any bugs or errors you come across.

Moreover, there will be many rough edges that may affect user experience, which we will work to improve. We hope to put together some comprehensive usage guides and other resources to help ease the barrier to entry and learning curve involved - if you're interested in helping with this, please reach out!

Additionally, this tool is not made to be a replacement for Anamnesis. Instead, it's intended to be a new addition to the standard toolkits of posers, and will best be used in conjunction.

## Installation

Ktisis is written as a Dalamud plugin and as such, requires that you use [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) to start your game.
<br/>
This will enable you to install community-created plugins.

1. Type the `/xlsettings` command into your chatbox. This will open your Dalamud Settings.
2. In the window that opens, navigate to the "Experimental" tab. Scroll down to "Custom Plugin Repositories".
3. Copy and paste the repo URL (seen below) into the input box, **making sure to press the "+" button to add it.**
4. Press the "Save and Close" button. This will add Ktisis to Dalamud's list of available plugins.
5. Open the plugin installer by typing the `/xlplugins` command, search for Ktisis, and click install.

#### Repo URL
`https://raw.githubusercontent.com/ktisis-tools/Ktisis/main/repo.json`

## Contributing

Contributions are generally welcome, as long as they adhere to the following principles:
- Manipulation of any ingame object must be confined within GPose.
- It must not automate any tasks that result in network packets being sent to the server.
- Any changes to the client state must not be:
  - a) Permanent or irreversible by the user.
  - b) Detectable by the server, directly or indirectly.

Ktisis makes heavy use of unsafe code. If you are inexperienced or unfamiliar with the risks of this, then refrain from making code contributions that depend on it. Pull requests that show a reckless disregard for memory safety may be closed without further review.

Pull requests containing new features must be reviewed by an organization member or repo maintainer before being merged - PRs heavily containing unsafe code must be reviewed by both core developers ([@chirpxiv](https://github.com/chirpxiv), [@Fayti1703](https://github.com/Fayti1703)).

As Ktisis is currently in a 'soft' feature lock with v0.3's codebase overhaul underway, PRs can take some time before they get merged or put in an official release; most will likely be put into individual branches to be merged with v0.3.

## Acknowledgements

Huge thanks go out to:
- [Goat Corp](https://github.com/goatcorp) and [XIV Tools](https://github.com/XIV-Tools), whose existing work in this area provided excellent insight into internal memory and data structures.
- [@BobTheBob9](https://github.com/BobTheBob9), [@Fayti1703](https://github.com/Fayti1703) and [@hackmud-dtr](https://github.com/hackmud-dtr) for their help with reverse engineering and low-level code.
- The other developers from [Bnuuy Central](https://github.com/Bnuuy-Central) who have supported and helped me through this project's development.
- perchird ([@lmcintyre](https://github.com/lmcintyre/)) for their amazing work on Havok structs which helped to make this possible.
- [@Emyka](https://github.com/Emyka) for their continued efforts to help with accessibility and customisation.
- Everyone who has contributed code and features through pull requests!
