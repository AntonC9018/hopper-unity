# Overview

This is supposed to be the `View` part for [this](https://github.com/AntonC9018/hopper.cs) project.

This particular View implementation involves Unity as the game engine.

This project binds Logic with Graphics. See a demo in the file `Assets/Content/Demo.cs` (also currently used for the scene).

# Setup

1. Get Unity version `2019.4.10f1` installed on your computer, which you can find [here](https://unity3d.com/unity/qa/lts-releases). I recommend using the torrent install option, it's much faster.

2. Clone this repo somewhere, using the command `git clone https://github.com/AntonC9018/hopper-unity FOLDER_NAME`.

3. Clone the repo with the model, using the command `git clone https://github.com/AntonC9018/hopper.cs FOLDER_NAME/Assets/Core`.

Similarly, use `git pull origin master` to get the newest version of code for the model.

Now you should be able to locate the project folder from unity and open the project.

After that, I recommend you make yourself a copy of my scene, since it is literally impossible to merge scenes in Unity via github without completely ditching one version of the two.

As for the editor, I'm using VS Code. If intellisense is missing for VS Code, follow [these steps](https://forum.unity.com/threads/intellisense-not-working-for-visual-studio-code.812040/#post-5858986). You may use any other editor, moreover I'm sure intellisense in Visual Studio and Rider works out of the box, it's just that these editors are more resource-demanding than VS Code so they may lag on weaker PC's.

# Architecture

No strict architecture exists yet, so I'm omitting this topic for now.