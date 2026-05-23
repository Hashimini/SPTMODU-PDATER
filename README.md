# MODU-PDATER
YOU NEED TO SET UP A SERVER OR USE https://github.com/Hashimini/SPTMODU-PDATERpanel 
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

http://192.168.0.15:8080

http://myserver.com:8080
```

### IMPORTANT

If you are using WAN or a VPN connection, use the Virtual IP instead.

## SPT Path

Root folder of your SPT installation.

Example:

```text
D:\SPT
```

After filling in the fields, click the SAVE button

---

# Usage

## Automatic verification

When opening the launcher it will:

* Check the Web Server connection
* Search for available patches
* Compare your local version with the server version

### With will result in one of those:

## Updated

```text
[ UPDATED ]
```

_No action is needed, you have the same version that the server._

## Update Available

```text
[ UPDATE AVAILABLE ]
```

_The launcher found pending patches._

The following information will be displayed:

* Latest available version
* Number of pending patches
* Changelog

Just click update then the launcher will automatically:

* Download the patches
* Remove obsolete files
* Extract the new files
* Update the local version

Do not close the launcher during the update process or you will lose the progress.

---

# Troubleshooting

## WEB SERVER OFFLINE

The launcher could not connect to the update server.

Check:

* Internet connection
* Firewall
* Configured IP
* Server port

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
