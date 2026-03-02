# New Input System Setup Guide

This project now uses the **Unity Input System Package** (new) instead of the legacy Input Manager.

## ✅ What's Been Updated

| File | Changes |
|------|---------|
| `PlayerController.cs` | Migrated to Input Actions - Movement, Attack, Skills, Inventory, Pause |
| `SkillCaster.cs` | Updated to use `Mouse.current.position` for mouse input |
| `UIManager.cs` | Removed duplicate input handling (now handled by PlayerController) |

---

## 📦 Setup Steps in Unity

### 1. Ensure Input System Package is Installed

1. Go to **Window > Package Manager**
2. Search for **Input System**
3. If not installed, install it
4. If prompted to restart Unity, click **Yes**

### 2. Set Active Input Handling

1. Go to **Edit > Project Settings > Player**
2. Under **Other Settings**, find **Active Input Handling**
3. Set to **Input System Package (New)** or **Both** (for compatibility)
   - **Recommended:** `Both` during transition, then `Input System Package (New)` once everything works

### 3. Assign Input Actions to Player

1. Select your **Player** GameObject in the scene
2. Find the **PlayerController** component
3. In the **Input** section, assign the `GameControls` asset:
   - Drag `Assets/InputSystem/GameControls.inputactions` to the **Input Actions** field

### 4. Generate C# Class (Optional but Recommended)

1. Select `Assets/InputSystem/GameControls.inputactions`
2. In Inspector, check **Generate C# Class**
3. Click **Apply**
4. This generates a C# wrapper for type-safe input access

---

## 🎮 Controls Reference

| Action | Input |
|--------|-------|
| **Move** | WASD or Arrow Keys |
| **Attack** | Space or Left Mouse Button |
| **Skill 1** | Key 1 |
| **Skill 2** | Key 2 |
| **Skill 3** | Key 3 |
| **Skill 4** | Key 4 |
| **Inventory** | I |
| **Pause** | Escape |

---

## 🔧 How It Works

### Input Action Asset Structure

```
GameControls.inputactions
├── Gameplay (Action Map)
│   ├── Move (Vector2) - WASD/ArrowKeys
│   ├── Attack (Button) - Space/LeftClick
│   ├── Skill1-4 (Button) - 1,2,3,4
│   ├── Inventory (Button) - I
│   └── Pause (Button) - Escape
└── UI (Action Map)
    ├── Navigate
    ├── Submit
    ├── Cancel
    ├── Point
    └── Click
```

### Code Flow

```
PlayerController.Awake()
    ↓
SetupInputActions() - Finds and caches actions from asset
    ↓
OnEnable() - gameplayActions.Enable()
    ↓
Input callbacks bound:
    - Attack.performed → OnAttackPerformed()
    - Skill1-4.performed → OnSkillPerformed(index)
    - Inventory.performed → OnInventoryPerformed()
    - Pause.performed → OnPausePerformed()
```

### Reading Movement (Continuous)

```csharp
void HandleInput() {
    // Read continuous value from Input Action
    moveInput = moveAction.ReadValue<Vector2>();
    // ...
}
```

### Handling Button Presses (Events)

```csharp
void OnAttackPerformed(InputAction.CallbackContext context) {
    TryMeleeAttack();
}
```

---

## 🧪 Testing Checklist

- [ ] Player moves with WASD
- [ ] Player moves with Arrow Keys
- [ ] Attack works with Space
- [ ] Attack works with Left Mouse Button
- [ ] Skills 1-4 work
- [ ] Inventory toggles with I
- [ ] Escape opens/closes pause menu
- [ ] Escape closes panels before pausing
- [ ] Mouse aiming works for skills

---

## 🐛 Troubleshooting

### "Input Action Asset not assigned!"
**Fix:** Drag `GameControls.inputactions` to PlayerController's Input Actions field

### "Could not find 'Gameplay' action map"
**Fix:** Ensure the input actions file has a "Gameplay" action map with matching action names

### Controls not responding
1. Check that `gameplayActions.Enable()` is called in `OnEnable()`
2. Verify **Active Input Handling** is set to include Input System
3. Check Console for errors

### Mouse position not working for skills
**Fix:** Ensure `Mouse.current` is not null. If using gamepad only, you may need to implement an alternative aiming method.

---

## 📝 Migration Notes

### Old vs New API

| Old (Input Manager) | New (Input System) |
|---------------------|-------------------|
| `Input.GetAxisRaw("Horizontal")` | `moveAction.ReadValue<Vector2>().x` |
| `Input.GetKeyDown(KeyCode.Space)` | `attackAction.performed += callback` |
| `Input.GetButtonDown("Fire1")` | `attackAction.performed += callback` |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` |
| `Input.GetKeyDown(KeyCode.I)` | `inventoryAction.performed += callback` |

### Adding New Actions

1. Open `GameControls.inputactions` in Unity
2. Click **Edit asset** (double-click)
3. Add new Action in the Gameplay map
4. Bind keys/buttons
5. Add field in `PlayerController.cs`:
   ```csharp
   private InputAction myNewAction;
   ```
6. Initialize in `SetupInputActions()`:
   ```csharp
   myNewAction = gameplayActions.FindAction("MyNewAction");
   ```
7. Bind callback in `SetupInputActions()`:
   ```csharp
   myNewAction.performed += OnMyNewActionPerformed;
   ```
8. Unsubscribe in `OnDestroy()`:
   ```csharp
   myNewAction.performed -= OnMyNewActionPerformed;
   ```

---

## 🎮 Adding Gamepad Support

The input actions are already set up to support gamepad. To add gamepad bindings:

1. Open `GameControls.inputactions`
2. Select an action (e.g., "Move")
3. Click **+** to add binding
4. Choose **Gamepad** as the path
5. Select the control (e.g., Left Stick)

The game will automatically work with both keyboard and gamepad!

---

*Input System migration complete!* 🎉
