# Build Setup Guide

**Date:** 2026-03-08  
**Version:** 1.0

---

## 📋 Table of Contents

1. [Overview](#1-overview)
2. [Build Settings Configuration](#2-build-settings-configuration)
3. [Player Settings](#3-player-settings)
4. [Platform-Specific Setup](#4-platform-specific-setup)
5. [Icons & Splash Screens](#5-icons--splash-screens)
6. [Quality Settings](#6-quality-settings)
7. [Audio Settings](#7-audio-settings)
8. [Build Automation](#8-build-automation)
9. [Testing Builds](#9-testing-builds)
10. [Distribution Checklist](#10-distribution-checklist)

---

## 1. Overview

This guide covers building CombatRun for different platforms with optimized settings.

### Supported Platforms

| Platform | Recommended For | Difficulty |
|----------|-----------------|------------|
| **Windows (PC)** | Primary platform | Easy |
| **macOS** | Apple users | Easy |
| **Linux** | Advanced users | Medium |
| **WebGL** | Browser play, demos | Medium |
| **Android** | Mobile players | Medium |
| **iOS** | iPhone/iPad users | Hard |

---

## 2. Build Settings Configuration

### Step 1: Open Build Settings

```
File → Build Settings (Ctrl+Shift+B)
```

### Step 2: Add Scenes

Drag scenes in this exact order:

| Index | Scene | Description |
|-------|-------|-------------|
| 0 | MainMenu | First scene loaded |
| 1 | Loading | Loading screen |
| 2 | Game | Main gameplay |

```
☑ Scenes/MainMenu.unity    (index 0)
☑ Scenes/Loading.unity     (index 1)
☑ Scenes/Game.unity        (index 2)
```

### Step 3: Select Platform

Click platform → "Switch Platform" (takes time):

```
PC, Mac & Linux Standalone ← Default
WebGL
Android
iOS
```

---

## 3. Player Settings

### Access Player Settings

```
Edit → Project Settings → Player
OR
Build Settings → Player Settings button
```

### PC/Mac/Linux Standalone Settings

#### Resolution & Presentation

```
Resolution:
  - Fullscreen Mode: Fullscreen Window (default)
  - Default Resolution: 1920x1080
  - Resolution Dialog: Disabled (handle in-game)

Standalone Player Options:
  - Render Outside Safe Area: [ ]
  - Resizable Window: [✓]
  - Visible in Background: [ ]
  - Allow Fullscreen Switch: [✓]
  - Force Single Instance: [ ] (allow multiple windows for testing)
```

#### Other Settings

```
Configuration:
  - Scripting Backend: IL2CPP (faster, harder to reverse)
  - Api Compatibility Level: .NET Standard 2.1

Publishing Settings:
  - Company Name: YourStudioName
  - Product Name: CombatRun
  - Version: 1.0.0

Splash Image:
  - Show Splash Screen: [✓] (or disable with Plus/Pro)
  - Draw Mode: Unity Logo Below
```

### WebGL Settings

#### Resolution & Presentation

```
Resolution:
  - Default Canvas Width: 1280
  - Default Canvas Height: 720

WebGL Template: Default (or Minimal)

Publishing Settings:
  - Compression Format: Gzip (smaller builds)
  - Name Files As Hashes: [✓] (better caching)
```

#### Other Settings

```
Configuration:
  - Scripting Backend: IL2CPP
  - Api Compatibility Level: .NET Standard 2.1
  - "exceptionSupport": "Full" (for debugging)

Optimization:
  - WebGL Memory Size: 256 (MB)
  - Enable Exceptions: Full (for debugging)
```

**⚠️ WebGL Limitations:**
- No threading support
- Limited file system access
- Audio may have latency
- Some effects may not work

### Android Settings

#### Resolution & Presentation

```
Resolution:
  - Default Orientation: Landscape Left
  - Allowed Orientations: [✓] Landscape Left, [✓] Landscape Right
```

#### Other Settings

```
Identification:
  - Package Name: com.yourstudio.combatrun
  - Version: 1.0 (1)
  - Minimum API Level: Android 8.0 (API 26)
  - Target API Level: Android 13.0 (API 33)

Configuration:
  - Scripting Backend: IL2CPP
  - Target Architectures: [✓] ARM64, [✓] ARMv7
  - Install Location: Prefer External

Optimization:
  - Stripping Level: Medium
  - Enable Engine Code Stripping: [✓]
```

### iOS Settings

#### Resolution & Presentation

```
Resolution:
  - Default Orientation: Landscape Left
  - Allowed Orientations: [✓] Landscape Left, [✓] Landscape Right
```

#### Other Settings

```
Identification:
  - Bundle Identifier: com.yourstudio.combatrun
  - Version: 1.0
  - Build: 1

Configuration:
  - Scripting Backend: IL2CPP
  - Target SDK: Device SDK
  - Target minimum iOS Version: 13.0
```

---

## 4. Platform-Specific Setup

### PC (Windows) Build

#### Recommended Settings

```yaml
Target Platform: Windows
Architecture: x86_64 (64-bit)
Build Type: Executable

Build Options:
  - Create Visual Studio Solution: [ ] (for debugging only)
  - Copy PDB files: [ ] (debugging)
  - Create Xcode Project: [ ]
```

#### Build Steps

1. Switch Platform to PC, Mac & Linux Standalone
2. Target Platform: Windows
3. Architecture: x86_64
4. Click "Build"
5. Choose folder: `Builds/Windows/`
6. Name: `CombatRun`

#### Output Files

```
Builds/Windows/
├── CombatRun.exe          ← Launch game
├── UnityPlayer.dll        ← Engine
├── CombatRun_Data/        ← Game data
│   ├── Managed/           ← Scripts
│   ├── Plugins/           ← Native plugins
│   ├── Resources/         ← Unity resources
│   └── StreamingAssets/   ← Game assets
```

### WebGL Build

#### Recommended Settings

```yaml
Compression Format: Gzip
Name Files As Hashes: true
Template: Default
n
Build Options:
  - Development Build: [ ] (uncheck for release)
```

#### Build Steps

1. Switch Platform to WebGL
2. Click "Build And Run" (opens browser)
3. Choose folder: `Builds/WebGL/`

#### Output Files

```
Builds/WebGL/
├── index.html             ← Entry point
├── Build/
│   ├── WebGL.loader.js
│   ├── WebGL.framework.js.gz
│   ├── WebGL.data.gz
│   └── WebGL.wasm.gz
└── TemplateData/          ← Unity logos, CSS
```

#### Hosting WebGL

Upload to:
- itch.io (free, easy)
- GitHub Pages (free)
- Netlify (free)
- Your own server

**⚠️ Important:** Enable gzip/Brotli compression on server!

### Android Build

#### Prerequisites

1. **Install Android Build Support**
   ```
   Unity Hub → Installs → Add Modules → Android Build Support
   ```

2. **Install Android SDK & NDK**
   ```
   Edit → Preferences → External Tools
   - Android SDK: Check "Installed with Unity"
   - Android NDK: Check "Installed with Unity"
   - JDK: Check "Installed with Unity"
   ```

3. **Enable Developer Mode on Device**
   ```
   Settings → About Phone → Tap "Build Number" 7 times
   Settings → Developer Options → USB Debugging: ON
   ```

#### Build Steps

1. Switch Platform to Android
2. Connect Android device via USB
3. Click "Build And Run"
4. Choose location: `Builds/Android/CombatRun.apk`

#### Output File

```
Builds/Android/
└── CombatRun.apk          ← Install on Android
```

#### App Bundle (Google Play)

For Google Play Store:
```
Build Settings → Build App Bundle (Google Play): [✓]
Output: CombatRun.aab
```

### iOS Build

#### Prerequisites

- macOS computer
- Xcode (latest version)
- Apple Developer Account ($99/year for App Store)

#### Build Steps

1. Switch Platform to iOS
2. Click "Build"
3. Choose folder: `Builds/iOS/`
4. Open generated Xcode project: `Builds/iOS/Unity-iPhone.xcodeproj`
5. In Xcode:
   - Set Team (Apple ID)
   - Set Bundle Identifier
   - Connect iPhone/iPad
   - Click Run

---

## 5. Icons & Splash Screens

### Icon Sizes Required

| Platform | Size | Usage |
|----------|------|-------|
| PC | 256x256, 128x128, 64x64, 32x32, 16x16 | Desktop icon |
| Android | 512x512 (Play Store), 192x192, 144x144, 96x96, 72x72, 48x36 | App icon |
| iOS | 1024x1024 (App Store), 180x180, 120x120, 87x87, 80x80, 60x60, 58x58, 40x40 | App icon |

### Setting Icons in Unity

```
Edit → Project Settings → Player → Icon

PC:
  - Select platform: PC, Mac & Linux Standalone
  - Drag 256x256 PNG to Default Icon
  
Android:
  - Select platform: Android
  - Enable "Adaptive icons" (for Android 8.0+)
  - Drag icons to all slots
  
iOS:
  - Select platform: iOS
  - Drag 1024x1024 to "iPhone/iPad Icon"
  - Unity generates all sizes
```

### Splash Screen

```
Edit → Project Settings → Player → Splash Image

Splash Screen:
  - Show Splash Screen: [✓]
  - Preview: [select]
  
Splash Style:
  - Draw Mode: Solid Color (faster) or Custom
  - Background Color: #000000 (black)
  
Splash Screen Logo:
  - Logos: Add your logo sprite
  - Animation: Unity Logo Below
```

**Custom Splash Screen Script:**

Create `Assets/Scripts/UI/CustomSplashScreen.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CustomSplashScreen : MonoBehaviour {
    public Image logoImage;
    public float fadeInDuration = 1f;
    public float displayDuration = 2f;
    public float fadeOutDuration = 1f;
    
    void Start() {
        StartCoroutine(ShowSplash());
    }
    
    IEnumerator ShowSplash() {
        // Fade in
        yield return StartCoroutine(Fade(0, 1, fadeInDuration));
        
        // Display
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        yield return StartCoroutine(Fade(1, 0, fadeOutDuration));
        
        // Load menu
        SceneTransitionManager.Instance?.GoToMainMenu();
    }
    
    IEnumerator Fade(float from, float to, float duration) {
        float elapsed = 0;
        Color color = logoImage.color;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            logoImage.color = color;
            yield return null;
        }
    }
}
```

---

## 6. Quality Settings

### Access Quality Settings

```
Edit → Project Settings → Quality
```

### Recommended Quality Levels

#### For PC (High-end)

```
Quality Level: Fantastic (or create custom)

Rendering:
  - Render Pipeline: URP (if using)
  - Pixel Light Count: 4
  - Texture Quality: Full Res
  - Anisotropic Textures: Forced On
  - Anti Aliasing: 4x Multi Sampling
  - Soft Particles: [✓]
  - Realtime Reflection Probes: [✓]
  
Shadows:
  - Shadow Resolution: High
  - Shadow Cascades: Four Cascades
  - Shadow Distance: 150
  
Other:
  - Blend Weights: 4 bones
  - V Sync Count: Every V Blank
  - LOD Bias: 2
  - Maximum LOD Level: 0
```

#### For WebGL/Mobile (Low-end)

```
Quality Level: Performant (or create custom)

Rendering:
  - Pixel Light Count: 1
  - Texture Quality: Half Res
  - Anisotropic Textures: Per Texture
  - Anti Aliasing: Disabled
  - Soft Particles: [ ]
  
Shadows:
  - Shadows: Hard Shadows Only
  - Shadow Resolution: Low
  - Shadow Cascades: No Cascades
  - Shadow Distance: 50
  
Other:
  - Blend Weights: 2 bones
  - V Sync Count: Don't Sync
  - LOD Bias: 1
```

### Auto-Quality Detection

Create `Assets/Scripts/Managers/QualityManager.cs`:

```csharp
using UnityEngine;

public class QualityManager : MonoBehaviour {
    void Start() {
        // Auto-detect quality based on platform
        #if UNITY_WEBGL
            QualitySettings.SetQualityLevel(0); // Fastest
        #elif UNITY_ANDROID || UNITY_IOS
            QualitySettings.SetQualityLevel(1); // Fast
        #else
            QualitySettings.SetQualityLevel(3); // Fantastic
        #endif
        
        Debug.Log($"Quality set to: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }
}
```

---

## 7. Audio Settings

### Import Settings

For all audio assets:

```
Select AudioClip → Inspector

Import Settings:
  - Load Type: Compressed In Memory (for music)
  - Load Type: Decompress On Load (for short SFX)
  - Compression Format: Vorbis (for music)
  - Compression Format: ADPCM (for SFX)
  - Quality: 50-70% (balance size/quality)
```

### Audio Mixer Setup

```
Window → Audio → Audio Mixer

Create Mixer Groups:
  - Master
    - Music
    - SFX
    - UI
```

Assign to sources:
```csharp
public class AudioManager : MonoBehaviour {
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;
    
    public void PlayMusic(AudioClip clip) {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = musicGroup;
        source.Play();
    }
}
```

---

## 8. Build Automation

### Build Script

Create `Assets/Editor/BuildAutomation.cs`:

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class BuildAutomation {
    
    [MenuItem("Build/Build All Platforms")]
    static void BuildAll() {
        BuildWindows();
        BuildWebGL();
        Debug.Log("All builds complete!");
    }
    
    [MenuItem("Build/Build Windows")]
    static void BuildWindows() {
        string path = "Builds/Windows/" + GetBuildName();
        
        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = GetScenes(),
            locationPathName = path + "/CombatRun.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(options);
        Debug.Log("Windows build complete: " + path);
    }
    
    [MenuItem("Build/Build WebGL")]
    static void BuildWebGL() {
        string path = "Builds/WebGL/" + GetBuildName();
        
        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = GetScenes(),
            locationPathName = path,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(options);
        Debug.Log("WebGL build complete: " + path);
    }
    
    [MenuItem("Build/Build Android")]
    static void BuildAndroid() {
        string path = "Builds/Android/" + GetBuildName() + ".apk";
        
        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = GetScenes(),
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        BuildPipeline.BuildPlayer(options);
        Debug.Log("Android build complete: " + path);
    }
    
    static string[] GetScenes() {
        return new string[] {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Loading.unity",
            "Assets/Scenes/Game.unity"
        };
    }
    
    static string GetBuildName() {
        return "CombatRun_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
    }
}
```

### Version Increment

```csharp
[MenuItem("Build/Increment Version")]
static void IncrementVersion() {
    string version = PlayerSettings.bundleVersion;
    string[] parts = version.Split('.');
    int build = int.Parse(parts[2]) + 1;
    PlayerSettings.bundleVersion = $"{parts[0]}.{parts[1]}.{build}";
    Debug.Log($"Version incremented to: {PlayerSettings.bundleVersion}");
}
```

---

## 9. Testing Builds

### Development vs Release

#### Development Build

```
Build Settings:
  - Development Build: [✓]
  - Script Debugging: [✓]
  - Deep Profiling: [ ] (only if needed)
  - Compression Method: LZ4 (faster builds)
```

**Use for:** Testing, debugging

#### Release Build

```
Build Settings:
  - Development Build: [ ]
  - Script Debugging: [ ]
  - Compression Method: LZ4HC (smaller, slower)
```

**Use for:** Distribution, publishing

### Pre-Build Checklist

- [ ] All scenes added to Build Settings
- [ ] Scenes in correct order (0: Menu, 1: Loading, 2: Game)
- [ ] Development Build unchecked (for release)
- [ ] Correct platform selected
- [ ] Player Settings configured (company, product name)
- [ ] Icons assigned
- [ ] Quality settings appropriate
- [ ] Test on target device

### Post-Build Testing

| Test | Windows | WebGL | Android | iOS |
|------|---------|-------|---------|-----|
| Game launches | [ ] | [ ] | [ ] | [ ] |
| MainMenu loads | [ ] | [ ] | [ ] | [ ] |
| Can start game | [ ] | [ ] | [ ] | [ ] |
| Controls work | [ ] | [ ] | [ ] | [ ] |
| Audio plays | [ ] | [ ] | [ ] | [ ] |
| No console errors | [ ] | [ ] | [ ] | [ ] |
| Performance good | [ ] | [ ] | [ ] | [ ] |
| Can quit properly | [ ] | [ ] | [ ] | N/A |

---

## 10. Distribution Checklist

### Before Release

- [ ] Test build on target platform
- [ ] Test on minimum spec hardware
- [ ] Check performance (60 FPS target)
- [ ] Verify all features work
- [ ] No debug logs in build
- [ ] All placeholder text removed
- [ ] Credits included

### PC Distribution

**Steam:**
- Create Steamworks account
- Pay $100 fee
- Upload build via SteamPipe
- Set store page

**itch.io:**
- Create account (free)
- Upload ZIP file
- Set price or free
- Optional: Add to Game Jam

**Direct:**
- Create ZIP of build folder
- Include README.txt
- Share download link

### Mobile Distribution

**Google Play (Android):**
- Google Play Developer account ($25 one-time)
- Create app listing
- Upload AAB file
- Set content rating
- Publish

**App Store (iOS):**
- Apple Developer account ($99/year)
- Create app in App Store Connect
- Upload via Xcode or Transporter
- Pass app review
- Publish

### Web Distribution

**itch.io:**
- Upload WebGL build folder as ZIP
- Set "Kind of project: HTML"
- Enable "This file will be played in the browser"

**Game Jolt:**
- Create game page
- Upload WebGL build
- Set to playable in browser

**Own Website:**
- Upload WebGL files to server
- Enable gzip/Brotli compression
- Link to index.html

---

## Quick Reference

### Build Folder Structure

```
Project/
├── Assets/              ← Source files
├── Builds/              ← Build outputs
│   ├── Windows/
│   ├── WebGL/
│   ├── Android/
│   └── iOS/
└── system set up guide/ ← Documentation
```

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Build Settings | Ctrl+Shift+B |
| Build & Run | Ctrl+B |
| Player Settings | Edit → Project Settings → Player |

### Build Sizes (Approximate)

| Platform | Size |
|----------|------|
| Windows | 100-200 MB |
| WebGL | 20-50 MB compressed |
| Android | 50-100 MB |
| iOS | 50-100 MB |

---

**Next Steps:**
1. Configure Player Settings
2. Set up icons
3. Test Development Build
4. Create Release Build
5. Distribute!
