MAGIC Unity Integration
=======================

[Unity](https://unity.com/) integration for the [MAGIC compiler](http://nas.sr/magic/).

This repository is built from the MAGIC repository and contains a user interface and API designed to work with the Unity Editor and C# scripting with a particular focus on iOS export.

API
---


This repository exposes its own API into the Clojure runtime in addition to the API provided by ClojureCLR (under the `clojure.lang.*` namespaces). When possible, our APIs should be preferred over the ClojureCLR APIs because they take into consideration Unity-specific issues.

The stable API is under the `Magic.Unity.Clojure` namespace. The experimental API is under the `Magic.Unity.Clojure.Alpha` namespace.

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
