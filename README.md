# MODU-PDATER

## What is MODU-PDATER?

MODU-PDATER is a launcher responsible for:

* Checking for new modpack versions
* Downloading incremental patches
* Automatically removing obsolete files
* Installing updates
* Displaying version changelogs
* Launching the SPT Launcher

This system was designed to simplify modpack deployment across multiple computers, avoiding complicated instructions for less experienced users while also automating the process of installing mods for other players.

---

# Installation

## 1. Extract the launcher

Extract the MODU-PDATER files to any folder you want.

Example:

```text
D:\MODU-PDATER
```

---

## 2. Run the launcher

Open:

```text
MODU-PDATER.exe
```

On the first launch, you will need to configure:

* Server IP/URL
* SPT installation path

Open the `SETTINGS` menu and fill in the fields below.

---

# Settings

## Server IP

Address of the server responsible for updates.

Valid examples:

```text
192.168.0.15:8080
```

```text
http://192.168.0.15:8080
```

```text
http://myserver.com:8080
```

### IMPORTANT

If you are using WAN or a VPN connection, use the Virtual IP instead.

---

## SPT Path

Root folder of your SPT installation.

Example:

```text
D:\SPT
```

After filling in the fields, click:

```text
SAVE
```

Your configuration will be stored locally.

---

# Usage

## Automatic verification

When opening the launcher, the system will:

* Check the Web Server connection
* Search for available patches
* Compare your local version with the server version

---

# Possible States

## Updated

```text
[ UPDATED ]
```

No action is required.

---

## Update Available

```text
[ UPDATE AVAILABLE ]
```

The launcher found pending patches.

The following information will be displayed:

* Latest available version
* Number of pending patches
* Combined changelog

Click:

```text
UPDATE
```

The launcher will automatically:

* Download the required patches
* Remove obsolete files
* Extract the new files
* Update the local version
* Clean temporary files

Do not close the launcher during the update process.

---

# Changelog

The launcher displays:

* Latest version changes
* Combined history of pending patches

Example:

```text
--- CHANGELOG v1.0.0 ---
[+] Added FIKA

--- CHANGELOG v1.1.0 ---
[+] Added Fontaine's FOV Fix 4.0.1
```

---

# Troubleshooting

## WEB SERVER OFFLINE

The launcher could not connect to the update server.

Check:

* Internet connection
* Firewall
* Configured IP
* Server port

---

## SPT SERVER OFFLINE

The SPT Server is currently offline.

This does not prevent updates from working.

---

## Failed to read versions.json

The version manifest is invalid or inaccessible.

Possible causes:

* Misconfigured server
* Corrupted file
* Incorrect URL

---

# Important Notes

* Do not manually move files during updates
* Do not change the SPT folder structure
* Avoid installing external mods
* The launcher uses incremental patches; staying on very old versions may increase update size

---

# Recommended Structure

```text
C:\SPT
├── BepInEx
├── SPT\user
```

---

# General Workflow

1. Open MODU-PDATER
2. Update if necessary
3. Open SPT Launcher
4. Play
