# Git Commit & Push Guide

How to commit your current project state to git.

---

## Step 1: Check Git Status

Open terminal/command prompt in project folder:

```bash
cd D:\Unity\UnityProjects\CombatRun
git status
```

You should see:
- New files (untracked)
- Modified files
- Deleted files

---

## Step 2: Stage All Changes

```bash
# Stage all changes
git add .

# Or stage specific files
git add Assets/Scripts/
git add Assets/Prefabs/
```

---

## Step 3: Commit Changes

```bash
# Commit with descriptive message
git commit -m "feat: Complete core gameplay setup with SPUM integration

- Add PlayerController with Input System
- Add Enemy AI with SPUM animations
- Add Skill System with 4 skill slots
- Add Inventory and Shop systems
- Add Weapon Mastery progression
- Add Lives and Revive mechanic
- Create all required prefabs
- Fix GoldPickup and damage number systems
- Add comprehensive documentation"
```

---

## Step 4: Push to Remote

If you have a remote repository:

```bash
# Push to main branch
git push origin main

# Or push to master branch
git push origin master
```

If no remote is set up:

```bash
# Add remote repository
git remote add origin https://github.com/yourusername/CombatRun.git

# Then push
git push -u origin main
```

---

## Step 5: Verify Push

```bash
# Check remote status
git status

# View commit log
git log --oneline -5
```

---

## What NOT to Commit

Your `.gitignore` should already exclude:
```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
*.csproj
*.unityproj
*.sln
*.user
*.pidb
*.booproj
```

**NEVER commit:**
- Library folder (large, auto-generated)
- Temp folder
- Build folders
- .log files

---

## If You Get Errors

### "Not a git repository"
```bash
git init
git add .
git commit -m "Initial commit"
```

### "Permission denied"
- Check your git credentials
- Make sure you have access to the repository

### "Merge conflicts"
```bash
# Pull latest changes first
git pull origin main

# Resolve conflicts, then commit
git add .
git commit -m "Merge branch 'main'"
git push
```

---

## Quick Reference

```bash
# Full workflow
git add .
git commit -m "Your message"
git push origin main

# Check status
git status

# View history
git log --oneline

# Undo last commit (keep changes)
git reset --soft HEAD~1

# Undo last commit (discard changes)
git reset --hard HEAD~1
```

---

## Good Commit Messages

**Format:**
```
type: Short description

Longer explanation if needed
- Bullet points for details
```

**Types:**
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation
- `refactor:` Code refactoring
- `test:` Tests
- `chore:` Maintenance

**Examples:**
```bash
git commit -m "feat: Add SPUM enemy animations"
git commit -m "fix: Enemy gold drop not spawning"
git commit -m "docs: Add setup guide for skills"
```

---

## After Commit/Push

Your project is now saved! You can:
1. Share the repository link
2. Clone on another machine
3. Roll back to this state if needed
4. Continue development safely

---

## Repository Structure

```
CombatRun/
├── .git/                 # Git metadata
├── .gitignore           # Ignore rules
├── Assets/              # Unity assets
│   ├── Scripts/         # C# scripts
│   ├── Prefabs/         # Prefabs
│   ├── Resources/       # ScriptableObjects
│   ├── InputSystem/     # Input actions
│   └── *.md             # Documentation
├── Packages/            # Unity packages
├── ProjectSettings/     # Unity settings
└── README.md            # Project readme
```

---

*Your project state is now preserved!*
