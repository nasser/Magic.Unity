MAGIC Unity Integration
=======================

[Unity](https://unity.com/) integration for the [MAGIC compiler](http://nas.sr/magic/).

This repository is built from the MAGIC repository and contains a user interface and API designed to work with the Unity Editor and C# scripting with a particular focus on iOS export.

To get started, clone the [example Unity project](https://github.com/nasser/Magic.Unity-Example) or follow the Overview section to use this repository in an existing Unity project.

Overview
--------
This integration offers an AOT based workflow to integrate Clojure into Unity. The basic workflow is as follows:

1. Clone this repository or download and extract its archive into your Unity project
2. Write Clojure your namespaces in `clj` files as usual, placing them under the Assets folder
3. Open the compiler window by navigating to MAGIC > Compiler...
4. Use the compiler window to configure your class path and choose the root namespaces to compile
5. Press the compile button
6. Write Unity components in C# as usual, using the a Magic.Unity API to require your Clojure namespaces and invoke your functions
7. Playing in the editor and exporting work as expected
8. If you change your Clojure source repeat step 5

User Interface
--------------
![](https://user-images.githubusercontent.com/412966/89140098-a3a55000-d50e-11ea-9e20-f382b8d39387.png)

This integration exposed the MAGIC compiler through a Unity editor window. This window can be opened from the main Unity menu by navigating to MAGIC > Compiler... It presents the following elements:

**Class Path** A list of paths to treat as the class path when compiling. These are the folders in which searches for namespaces begin. They are relative to the Unity project folder (the folder that contains the Assets folder). Entries can be removed with the `-` button to their right and new entries can be added with the large `+` button at the bottom of the section.

In the screenshot above "Assets" is the sole entry (the Assets folder is not currently added to the class path by default), indicating that searches for namespaces should begin in the Assets folder.

**Namespaces** A list of Clojure namespaces to compile. Each namespace will recursively compile its dependencies, so you only need to list "root" namespaces here. Entries can be removed with the `-` button to their right and new entries can be added with the large `+` button at the bottom of the section.

In the screenshot above the "boids" is the sole entry, indicating that the boids namespace and all its dependencies should be compiled.

**Advanced** The Advanced section exposes aspects of the compiler that are not commonly needed. These include the output folder of the compiled namespaces, verbose compilation, and control over [`link.xml`](https://docs.unity3d.com/Manual/ManagedCodeStripping.html#LinkXML).

**Compile** Pressing the compile button will cause MAGIC to search the given class path and compile the given Clojure namespaces. The compiled code is now useable from C# scripts and will work in play mode and in export. If you edit the Clojure sources you must press the compile button again.

You may have to toggle away from Unity application and back in order for it to register the newly compiled code. 

API
---
This repository exposes its own API into the Clojure runtime in addition to the API provided by ClojureCLR (under the `clojure.lang.*` namespaces). When possible, our APIs should be preferred over the ClojureCLR APIs because they take into consideration Unity-specific issues.

The API is contained within the `Magic.Unity.Clojure` static class.

### `void Magic.Unity.Clojure.Require(string ns)`

Required a Clojure namespace. This must be done before looking up any vars in the namespace.

### `clojure.lang.Var Magic.Unity.Clojure.GetVar(string ns, string name)`

Looks up and returns a Clojure var in a namespace. This var can be dereferenced with `deref` or invoked with `invoke`.

### `void Magic.Unity.Clojure.Boot()`

Initializes the Clojure runtime. This is done automatically by the other API methods and generally does not need to be called by end users.


Legal
-----
Copyright © 2020 Ramsey Nasser and contributors

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

```
http://www.apache.org/licenses/LICENSE-2.0
```

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
