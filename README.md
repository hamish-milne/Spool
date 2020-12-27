# Spool

A Twine 2 runtime for Unity. Currently a work-in-progress, and under active development.

See [the TODO file](./TODO) for current progress.

Spool allows you to play [Twine 2](https://twinery.org/2) stories in [Unity](https://unity3d.com) projects. Simply drop your HTML file into the Assets folder, select the asset in a Spool Output, and you're good to go. Links in the story will become clickable regions in the UGUI Text or TextMeshPro component it's attached to.

Spool is functionally quite similar to [Cradle](https://github.com/daterre/Cradle), which is also a Twine 2 library for Unity, but the priorities and design philosophies of the two are a little different. At the time of writing, Cradle is not being actively developed, but you should check it out anyway in case it fits your needs better.

Spool has the following design goals:

* Support the latest version (3.1.0) of Harlowe (the default story format) as standard.
* Compatibility with Twine/Harlowe as close to 100% as possible. If it works in the browser, it should work in Spool.
* Flexibility. It's easy to code your own Output to create a different UI - a dialogue wheel, for example.
* Game integration. Custom macros and external hooks allow your game to respond to the story text and vice-versa, for example to show character busts on-screen, or start a combat sequence in response to a story event.
* Moddability. Since Spool parses the Twine HTML file at runtime, it's easy to allow end users to mod in their own changes.
* As far as possible, Spool will support hot-reloading in Unity.

## Limitations

Right now, Spool is not compatible with AOT platforms (iOS, WebGL) due to a limitation of its dependency Lexico (which makes use of code generation). This is something I plan to fix soon.

A number of Harlowe macros are deliberately left unimplemented due to their relative complexity, or not really making sense in a Unity environment. They are left open for user implementation, however. These include:
* All the 'URL' macros, aside from (reload:)
* All the 'transition' macros

Some other macros will, though executing faithfully, be ignored by the included Unity Outputs. These are:
* (css:)
* (background:)
* (text-rotate:)
* Most of the (text-style:) options

They will still be passed to the Cursor as `<rotate=45>` or similar, but actually implementing text rotation is left as an exercise to the reader ;)

In general, Spool will never be bit-for-bit identical to Harlowe. In particular, no guarantees are made as to the nature of error messages, or behaviour not defined in the Harlowe manual.

While performance has been considered during development, the inherently dynamic nature of Harlowe means that there will always be some allocations when rendering a new passage. Furthermore, certain operations like `(history:)` have `O(n)` complexity, and will become slower as more passages are visited.

## Changes and fixes

Spool aims to be as compatible as possible with the Harlowe engine, so - where feasible - quirks of Harlowe are preserved. However, some behaviour is inconsistent with the manual (i.e. a bug) while others are so... specific to Harlowe's rendering code that to replicate it would be a detriment.

For example,`print(``$foo``)` produces `VarRef.create(State.variables,"foo").get()` in Twine, but produces `$foo` in Spool. Similarly, `(print: ``*foo*``)` produces an error in Twine, but correctly prints `*foo*` in Spool.

## Project structure

* **Spool**: The platform-independent library, targeting `netstandard2.0`. You can use this in other C#-based engines, besides Unity.
* **Spool.Test**: xUnit test code, mainly comprised of lots of snippets copied from the Harlowe manual. Since there's a pre-existing spec, Spool uses test-driven-development as much as possible.
* **Spool.Unity**: The Unity package (UPM - not .unitypackage), including Unity importers and runtime components.
* **Lexico**: A parser-generator library used as a dependency. I'm making a few changes throughout development, hence it's a submodule rather than a nuget package.

## Building

Right now, to include Spool in your project you'll need to build it yourself. Fortunately this is quite easy - just clone the repo and run the 'publish' VSCode task to copy the library dependencies into the Unity package, then reference Spool.Unity via the Packages window (or copy it into the Packages folder in your project). In future there will be tagged releases to allow direct referencing with UPM's Git syntax.

## Terminology

* **Spool**: Both the name of the project as a whole, and the base .NET library component. Also a 'thing you put Twine on'...
* **Story**: The output from Twine, a collection of 'passages'. Stories are 'played' in the browser or Spool.
* **Story format**: Twine's name for the 'languages' you can use to write the logic in stories. Spool supports the Harlowe format as standard.
* **Player**: The Unity component responsible for story state and coordination.
* **Output**: A Unity component that turns the abstract passage content into visible, interactable objects.
* **Cursor**: Emits, reads, and revises the passage output (analogous to a cursor in a text editing app)
* **XNode**: The root type of the `System.Xml.Linq` framework. Spool uses XNodes to keep track of the currently rendered text.