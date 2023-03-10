# Developer guide

ArcViewer is developed with Unity **2021.3.20f1** in C#.

## Development Environment Setup
* Clone the project from GitHub to your local work folder.
* Download and install [Unity Hub](https://unity3d.com/get-unity/download).
* Activate your license within Unity Hub. Most people should be eligible for a free Personal license.
* Install unity version 2021.3.20f1 from within Unity Hub in the "Installs" section. This may not be the latest release; in this case, you should consult the "download archive" (from within Unity Hub). The installer will take care of the required dependencies as well.
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.
* Open the project. The Unity editor should launch, and the project dependencies should download automatically.

## Running the Project
- Select "File" -> "Build and Run" within Unity.

## Repository Layout
- The `master` branch contains the latest stable code. Generally, you should not make commits or PRs directly to this branch.

- The `dev` branch contains the latest unstable code. This is the branch where development takes place, and is where contributions should be made.

- The `deploy` branch contains build files for the webpage deployed with GitHub Pages. This is not a branch for source code or development, and contributions should not be made here.

# Contributing

Any contributions to help develop ArcViewer are welcome. This section will contain some helpful info about effectively contributing.

## Bug Reports

You don't need to be a developer to contribute! Making good bug reports can help issues get fixed promptly.

### What to Include

A good bug report is one that clearly and concisely communicates the issue, and contains the necessary resources to diagnose.

Every bug report should have:
* A clear description of the issue
* Ideally, reliable steps to reproduce the issue
* The ArcViewer version - visible in the bottom left corner or in the "more info" panel (**NOTE:** Before reporting bugs you should ALWAYS make sure you're using the latest version of the app)
* The build platform - if you're using the standalone app, this will be the OS you're using, i.e. Windows, Linux, etc. If you're using the web app, you should specify which browser you're using.
* Depending on the issue, logs may also be needed. Logs can be shared through sites like paste.ee or Pastebin.com. If you're using the web app, the logs can be found by pressing ctrl + shift + i in your browser, and clicking the `console` tab. For standalone, the log file location will vary.
  * Windows: `(YOUR USER)\AppData\LocalLow\AllPolanDev\ArcViewer\Player.log`
    * the `AppData` folder may be hidden. To find it, check View -> Hidden items in file explorer.
  * Linux: `/.config/unity3d/AllPolanDev/ArcViewer/Player.log`

Please report bugs to [the issues page](https://github.com/AllPoland/ArcViewer/issues) using the `Bug report` template. Please also check for duplicate issues before reporting.

## Pull Requests

Contributions are welcome and appreciated, but please adhere to the code styling outlined in the `.editorconfig` included with the source code. The codebase differs from canonical formatting in some ways (because I'm super cool and epic and not like the other girls :sunglas:)

Visual Studio is the recommended IDE for contributions, because of its strong .NET language support and native `.editorconfig` support. You're free to use other IDEs but support will vary.

Pretty much all PRs should be made to the `dev` branch.