# ADR 0001: Godot, 3D greybox, iOS-first

Status: accepted — 2026-08-31

## Context

This is a first game project. The target is an isometric weapon-and-style action game, and the largest early risk is combat readability and feel rather than content quantity or final visual fidelity.

## Decision

Use Godot 4.7.2 Standard with GDScript and the Mobile renderer. Build a real 3D greybox from procedural primitives. Develop on desktop, then export through macOS and Xcode for iOS device testing.

## Consequences

Positive:

- no engine subscription or runtime royalty;
- text-heavy source files work well with Git and coding agents;
- no large binary dependency is required for M0;
- one project can test desktop and mobile input.

Negative:

- final character modelling, rigging and animation still require a production pipeline;
- iOS export requires macOS, Xcode, signing and device testing;
- primitive visuals cannot validate final aesthetic appeal.
