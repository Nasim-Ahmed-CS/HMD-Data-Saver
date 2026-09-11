# HMD Data Saver

This repository contains Unity C# scripts for collecting HMD-based research data, including eye tracking, head tracking, collision events, memory tasks, object-search tasks, SAGAT assessments, and device-frame capture.

## Project Structure

### hmdDatasavers/
Original Unity script files collected from the project.
- `CollisionCounter.cs` — logs hazard collision events to CSV
- `MemoryTask.cs` — handles memory task prompts, timing, and answer logging
- `ObjectManagement.cs` — tracks searched objects and final object counts
- `SAGATManager.cs` — manages SAGAT questionnaire flow and response logging
- `UnifiedTrackingManager.cs` — central tracking logic for the full study flow

### unified-scripts/
A grouped copy of the original scripts kept in one folder for quick review and upload.
- Useful when you want the script set together in a single place.
- Keeps the original logic intact for easier reference.

### modular-scripts/
A refactored version split by responsibility for cleaner maintenance and future extension.
- `Core/` — shared participant/session state and CSV utilities
- `Tracking/` — eye tracking, head tracking, and gaze-target logic
- `Tasks/` — collision, memory, object-search, and SAGAT task logic
- `Bootstrap/` — runtime wiring for tracking components
- `device-frame-capture/` — captures camera/device frames to disk for later analysis

### device-frame-capture/
Located inside `modular-scripts/` for consistency with the refactored structure.
- `DeviceFrameCapture.cs` — captures camera frames and saves them as image files
- Useful for visual logging, passthrough capture, or later replay/analysis

## Purpose
The project is designed to record participant behavior and performance in immersive or HMD-based experiments. It supports analysis of attention, collisions, task accuracy, head movement, eye-gaze behavior, and recorded visual frames.

## Notes
This repository is intended as a research and development codebase, with both the original scripts and newer modular/experimental utilities for easier maintenance, extension, and collaboration.
