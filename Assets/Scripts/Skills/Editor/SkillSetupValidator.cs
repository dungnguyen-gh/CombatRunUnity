#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility for validating skill setup in the scene.
/// Helps identify missing skills, prefabs, and configuration issues.
/// 
/// === USAGE ===
/// - Select a GameObject with SkillCaster in the scene
/// - Click "Validate Skill Setup" in the inspector
/// - Or use Menu: Tools > Skill System > Validate All Skill Setups
/// </summary>
[CustomEditor(typeof(SkillCaster))]
public class SkillCasterEditor : Editor {
    bool showValidation = true;
    bool showSetup = true;
    
    public override void OnInspectorGUI() {
        serializedObject.Update();
        
        var skillCaster = (SkillCaster)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Validation Section
        showValidation = EditorGUILayout.Foldout(showValidation, "Validation", true, EditorStyles.foldoutHeader);
        if (showValidation) {
            EditorGUILayout.Space(5);
            
            // Quick validate button
            if (GUILayout.Button("Validate Skill Setup", GUILayout.Height(30))) {
                ValidateSkillCaster(skillCaster);
            }
            
            EditorGUILayout.Space(5);
            
            // Show validation status
            ShowValidationStatus(skillCaster);
        }
        
        EditorGUILayout.Space(10);
        
        // Quick Setup Section
        showSetup = EditorGUILayout.Foldout(showSetup, "Quick Setup", true, EditorStyles.foldoutHeader);
        if (showSetup) {
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Auto-Assign References", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Find/Create Cast Point")) {
                FindOrCreateCastPoint(skillCaster);
            }
            
            if (GUILayout.Button("Setup Layer Masks")) {
                SetupLayerMasks(skillCaster);
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset All Cooldowns (Play Mode Only)")) {
                if (Application.isPlaying) {
                    skillCaster.ResetAllCooldowns();
                    EditorUtility.DisplayDialog("Success", "All cooldowns reset!", "OK");
                } else {
                    EditorUtility.DisplayDialog("Error", "Only available in Play Mode!", "OK");
                }
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void ShowValidationStatus(SkillCaster caster) {
        var issues = GetValidationIssues(caster);
        
        if (issues.Count == 0) {
            EditorGUILayout.HelpBox("✓ Skill setup is valid!", MessageType.Info);
            return;
        }
        
        int errors = issues.Count(i => i.type == ValidationIssueType.Error);
        int warnings = issues.Count(i => i.type == ValidationIssueType.Warning);
        
        string summary = $"Found {errors} error(s) and {warnings} warning(s):";
        EditorGUILayout.HelpBox(summary, errors > 0 ? MessageType.Error : MessageType.Warning);
        
        EditorGUILayout.Space(5);
        
        foreach (var issue in issues) {
            var style = new GUIStyle(EditorStyles.label);
            style.wordWrap = true;
            
            var icon = issue.type == ValidationIssueType.Error 
                ? EditorGUIUtility.IconContent("console.erroricon.sml") 
                : EditorGUIUtility.IconContent("console.warnicon.sml");
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(issue.message, style);
            EditorGUILayout.EndHorizontal();
        }
    }
    
    List<ValidationIssue> GetValidationIssues(SkillCaster caster) {
        var issues = new List<ValidationIssue>();
        
        // Check skills array
        if (caster.skills == null || caster.skills.Length != 4) {
            issues.Add(new ValidationIssue {
                type = ValidationIssueType.Error,
                message = "Skills array must have exactly 4 slots (for keys 1-4)"
            });
        } else {
            // Check each skill slot
            for (int i = 0; i < caster.skills.Length; i++) {
                var skill = caster.skills[i];
                
                if (skill == null) {
                    issues.Add(new ValidationIssue {
                        type = ValidationIssueType.Warning,
                        message = $"Slot {i + 1} (Key {i + 1}): No skill assigned"
                    });
                    continue;
                }
                
                // Validate skill configuration
                if (!skill.Validate(out string error)) {
                    issues.Add(new ValidationIssue {
                        type = ValidationIssueType.Error,
                        message = $"Slot {i + 1} ({skill.skillName}): {error}"
                    });
                }
                
                // Check required prefabs based on skill type
                switch (skill.skillType) {
                    case SkillType.Projectile:
                        if (skill.projectilePrefab == null && caster.defaultProjectilePrefab == null) {
                            issues.Add(new ValidationIssue {
                                type = ValidationIssueType.Error,
                                message = $"Slot {i + 1} ({skill.skillName}): Projectile skill needs projectilePrefab " +
                                         "(in SkillSO) or defaultProjectilePrefab (on SkillCaster)"
                            });
                        }
                        break;
                        
                    case SkillType.Summon:
                        if (skill.summonPrefab == null) {
                            issues.Add(new ValidationIssue {
                                type = ValidationIssueType.Error,
                                message = $"Slot {i + 1} ({skill.skillName}): Summon skill needs summonPrefab"
                            });
                        }
                        break;
                }
                
                // Check for icon
                if (skill.icon == null) {
                    issues.Add(new ValidationIssue {
                        type = ValidationIssueType.Warning,
                        message = $"Slot {i + 1} ({skill.skillName}): No icon assigned - UI will show empty"
                    });
                }
            }
        }
        
        // Check references
        if (caster.castPoint == null) {
            issues.Add(new ValidationIssue {
                type = ValidationIssueType.Warning,
                message = "castPoint is not assigned (will use player transform)"
            });
        }
        
        if (caster.player == null) {
            issues.Add(new ValidationIssue {
                type = ValidationIssueType.Warning,
                message = "player reference not assigned (will auto-find)"
            });
        }
        
        // Check layers
        if (caster.enemyLayer == 0) {
            issues.Add(new ValidationIssue {
                type = ValidationIssueType.Warning,
                message = "enemyLayer is not set - skills won't detect enemies"
            });
        }
        
        return issues;
    }
    
    void ValidateSkillCaster(SkillCaster caster) {
        var issues = GetValidationIssues(caster);
        
        if (issues.Count == 0) {
            EditorUtility.DisplayDialog("Validation Passed", 
                "Skill setup is valid! All skills are properly configured.", "OK");
        } else {
            int errors = issues.Count(i => i.type == ValidationIssueType.Error);
            EditorUtility.DisplayDialog("Validation Issues", 
                $"Found {issues.Count} issue(s) including {errors} error(s).\n\n" +
                "Check the Validation foldout in the inspector for details.", "OK");
        }
    }
    
    void FindOrCreateCastPoint(SkillCaster caster) {
        // Look for existing cast point
        Transform castPoint = caster.transform.Find("CastPoint");
        
        if (castPoint == null) {
            // Create new cast point
            var go = new GameObject("CastPoint");
            go.transform.SetParent(caster.transform, false);
            go.transform.localPosition = new Vector3(0, 0.5f, 0); // Slightly above player
            castPoint = go.transform;
            
            EditorGUIUtility.PingObject(go);
            Selection.activeGameObject = go;
        }
        
        // Assign to caster
        Undo.RecordObject(caster, "Assign Cast Point");
        caster.castPoint = castPoint;
        EditorUtility.SetDirty(caster);
        
        EditorUtility.DisplayDialog("Success", 
            "CastPoint assigned! Position it where projectiles should spawn.", "OK");
    }
    
    void SetupLayerMasks(SkillCaster caster) {
        Undo.RecordObject(caster, "Setup Layer Masks");
        
        // Try to find Enemy layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) {
            caster.enemyLayer = 1 << enemyLayer;
        }
        
        // Try to find Obstacle layer
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0) {
            caster.obstacleLayer = 1 << obstacleLayer;
        } else {
            // Try Wall layer
            obstacleLayer = LayerMask.NameToLayer("Wall");
            if (obstacleLayer >= 0) {
                caster.obstacleLayer = 1 << obstacleLayer;
            }
        }
        
        EditorUtility.SetDirty(caster);
        
        string message = "Layer masks set!\n\n";
        message += $"Enemy Layer: {(caster.enemyLayer != 0 ? "Set" : "Not found - create 'Enemy' layer")}\n";
        message += $"Obstacle Layer: {(caster.obstacleLayer != 0 ? "Set" : "Not found - create 'Obstacle' or 'Wall' layer")}";
        
        EditorUtility.DisplayDialog("Layer Masks", message, "OK");
    }
    
    enum ValidationIssueType { Error, Warning }
    
    class ValidationIssue {
        public ValidationIssueType type;
        public string message;
    }
}

/// <summary>
/// Shared validation types for skill system.
/// </summary>
public enum SkillValidationIssueType { Error, Warning }

public class SkillValidationIssue {
    public SkillValidationIssueType type;
    public string message;
}

/// <summary>
/// Menu items for skill system validation and setup.
/// </summary>
public static class SkillSetupValidator {
    
    /// <summary>
    /// Gets validation issues for a SkillCaster. Shared helper for batch validation.
    /// </summary>
    public static List<SkillValidationIssue> GetValidationIssuesForCaster(SkillCaster caster) {
        var issues = new List<SkillValidationIssue>();
        
        // Check skills array
        if (caster.skills == null || caster.skills.Length != 4) {
            issues.Add(new SkillValidationIssue {
                type = SkillValidationIssueType.Error,
                message = "Skills array must have exactly 4 slots (for keys 1-4)"
            });
        } else {
            // Check each skill slot
            for (int i = 0; i < caster.skills.Length; i++) {
                var skill = caster.skills[i];
                
                if (skill == null) {
                    issues.Add(new SkillValidationIssue {
                        type = SkillValidationIssueType.Warning,
                        message = $"Slot {i + 1} (Key {i + 1}): No skill assigned"
                    });
                    continue;
                }
                
                // Validate skill configuration
                if (!skill.Validate(out string error)) {
                    issues.Add(new SkillValidationIssue {
                        type = SkillValidationIssueType.Error,
                        message = $"Slot {i + 1} ({skill.skillName}): {error}"
                    });
                }
                
                // Check required prefabs based on skill type
                switch (skill.skillType) {
                    case SkillType.Projectile:
                        if (skill.projectilePrefab == null && caster.defaultProjectilePrefab == null) {
                            issues.Add(new SkillValidationIssue {
                                type = SkillValidationIssueType.Error,
                                message = $"Slot {i + 1} ({skill.skillName}): Projectile skill needs projectilePrefab"
                            });
                        }
                        break;
                        
                    case SkillType.Summon:
                        if (skill.summonPrefab == null) {
                            issues.Add(new SkillValidationIssue {
                                type = SkillValidationIssueType.Error,
                                message = $"Slot {i + 1} ({skill.skillName}): Summon skill needs summonPrefab"
                            });
                        }
                        break;
                }
                
                // Check for icon
                if (skill.icon == null) {
                    issues.Add(new SkillValidationIssue {
                        type = SkillValidationIssueType.Warning,
                        message = $"Slot {i + 1} ({skill.skillName}): No icon assigned"
                    });
                }
            }
        }
        
        // Check references
        if (caster.castPoint == null) {
            issues.Add(new SkillValidationIssue {
                type = SkillValidationIssueType.Warning,
                message = "castPoint is not assigned (will use player transform)"
            });
        }
        
        if (caster.enemyLayer == 0) {
            issues.Add(new SkillValidationIssue {
                type = SkillValidationIssueType.Warning,
                message = "enemyLayer is not set - skills won't detect enemies"
            });
        }
        
        return issues;
    }
    
    [MenuItem("Tools/Skill System/Validate All Skill Setups")]
    static void ValidateAllSkillSetups() {
        var skillCasters = Object.FindObjectsByType<SkillCaster>(FindObjectsSortMode.None);
        
        if (skillCasters.Length == 0) {
            EditorUtility.DisplayDialog("No SkillCasters", 
                "No SkillCaster components found in the scene.", "OK");
            return;
        }
        
        int totalErrors = 0;
        int totalWarnings = 0;
        
        foreach (var caster in skillCasters) {
            // Count issues for summary
            var issues = GetValidationIssuesForCaster(caster);
            totalErrors += issues.Count(i => i.type == SkillValidationIssueType.Error);
            totalWarnings += issues.Count(i => i.type == SkillValidationIssueType.Warning);
            EditorGUIUtility.PingObject(caster);
        }
        
        string message = $"Validated {skillCasters.Length} SkillCaster(s).\n\n";
        if (totalErrors > 0 || totalWarnings > 0) {
            message += $"Total: {totalErrors} error(s), {totalWarnings} warning(s)\n\n";
        } else {
            message += "All skill setups are valid! ✓\n\n";
        }
        message += "Select each SkillCaster in the hierarchy to see detailed validation results.";
        
        EditorUtility.DisplayDialog("Validation Complete", message, "OK");
    }
    
    [MenuItem("Tools/Skill System/Create Skill Data Asset")]
    static void CreateSkillDataAsset() {
        // Create the SkillSO asset
        var skill = ScriptableObject.CreateInstance<SkillSO>();
        skill.skillId = "new_skill";
        skill.skillName = "New Skill";
        skill.cooldownTime = 5f;
        skill.range = 5f;
        skill.radius = 2f;
        
        // Save to Assets folder
        string path = "Assets/Data/Skills/Skill_NewSkill.asset";
        
        // Ensure directory exists
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorGUIUtility.PingObject(skill);
        Selection.activeObject = skill;
        
        Debug.Log($"[SkillSetupValidator] Created new skill asset at: {path}");
    }
    
    [MenuItem("Tools/Skill System/Documentation")]
    static void ShowDocumentation() {
        EditorUtility.DisplayDialog("Skill System Documentation",
            "=== SKILL SETUP GUIDE ===\n\n" +
            "1. Create SkillSO assets via:\n" +
            "   Right-click > Create > ARPG > Skill\n\n" +
            "2. Assign skills to SkillCaster:\n" +
            "   - Select Player GameObject\n" +
            "   - Find SkillCaster component\n" +
            "   - Drag SkillSO to skills array (4 slots)\n\n" +
            "3. Required References:\n" +
            "   - castPoint: Where projectiles spawn\n" +
            "   - enemyLayer: LayerMask for enemies\n" +
            "   - projectilePrefab (for projectile skills)\n\n" +
            "4. Input System:\n" +
            "   - Uses Unity's NEW Input System\n" +
            "   - Keys 1-4 mapped to Skill1-4 actions\n\n" +
            "5. Testing:\n" +
            "   - Skills work without SPUM\n" +
            "   - Set useSPUM=false in PlayerController",
            "OK");
    }
}
#endif
