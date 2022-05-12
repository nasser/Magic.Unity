(ns dotnet

  "Dotnet related tasks to be called by `nostrand`.
  Nostrand uses the `magic` compiler.

  ## Motivation

  This namespace provides convenient functions to:
  - pack and push NuGet Packages to a host repo"

  (:require [nostrand.tasks :as tasks]))

(defn nuget-push
  "Pack and Push NuGet Package to git host repo.
  nos dotnet/nuget-push"
  []
  (tasks/nuget-push "github" false "Release"))
