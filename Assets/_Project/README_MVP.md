# Absurd Liminal Expedition Horror — MVP Vertical Slice

Scene: `Assets/_Project/Scenes/MVPVerticalSlice.unity`

This is a self-contained MVP slice generated at runtime by `MvpVerticalSliceBootstrap`.
The scene itself stays almost empty to avoid fragile hand-edited Unity scene references.

## Controls

- WASD: move
- Mouse: look
- Shift: sprint
- Ctrl or C: crouch
- Space: jump
- E: interact
- F: flashlight
- Left mouse: melee pipe swing
- Esc: unlock or lock cursor
- R: restart BR-01 after fail

## Implemented MVP loop

1. Hub with procedure terminal, archive note, storage, and zone portal.
2. Backrooms-like BR-01 zone.
3. Objectives: find unstable room, record audio phenomenon, activate beacon, extract.
4. Zone rule: do not move during siren.
5. Danger meter and procedural audio tension.
6. One threat: Corridor Listener.
7. Basic melee: left mouse stuns the threat if it is close and in front of the camera.
8. Extraction returns to hub and saves a completion flag in PlayerPrefs.

## Notes

- No new Input System migration was done; the slice uses `Input.GetKey` and old mouse axes.
- No third-party assets are required.
- Geometry, UI, lights, procedural audio, and interactables are created by code at runtime.
- The scene is added after `EntryPointBootstrap` and `MainMenu1` in Build Settings so existing bootstrap/menu scenes remain in place.
