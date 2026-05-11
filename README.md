# Arise Tool

###  Solo Leveling: ARISE asset extraction tool

![C#](https://img.shields.io/badge/C%23-.NET-purple?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=for-the-badge)
![Status](https://img.shields.io/badge/status-active-success?style=for-the-badge)
![Header](SL_Asset_Extractor.UI\SL_Asset_Extractor.UI\header.jpg)

---

## About

**Arise Tool** is a Windows desktop application designed to automatically extract, classify and organize assets from **Solo Leveling: ARISE** Unity bundles.

The software handles the full extraction pipeline — from bundle scanning to organized PNG export — with no manual intervention required after initial setup.

This project was built as a first C# application, using a game I genuinely enjoy as a practical context to learn:

- Software architecture and modular design
- Asynchronous programming and multithreading
- File I/O and Unity bundle processing
- SQLite databases and indexing
- Desktop UI development with WPF

---

## Interface

![Interface](SL_Asset_Extractor.UI\SL_Asset_Extractor.UI\Interface.png)

---

## Features

| Category | Details |
|---|---|
| Extraction | Incremental extraction, bundle scanning, duplicate prevention |
| Detection | New and modified bundle detection via SHA256 hashing |
| Supported Assets | Texture2D, Sprite |
| Organization | Automatic classification into 40+ named categories |
| Database | SQLite indexing, per-bundle hash tracking |
| UI | Real-time logs, progress bar, time estimation |
| Workflow | Multi-source folder support, persistent settings |

---

## AssetStudio Integration

Arise Tool uses the CLI version of AssetStudio developed by:

[astro75 — AssetStudioMod](https://github.com/astro75/AssetStudioMod)

The extraction engine would not be possible without the work of the AssetStudio team and its contributors.

---

## Requirements

| Requirement | Version |
|---|---|
| Operating System | Windows 10 / 11 (64-bit) |
| Runtime | .NET 8 Runtime |
| Game | Solo Leveling: ARISE (PC) |
| Extraction Engine | AssetStudioModCLI (included) |

---

## Quick Start

**1. Download and launch**

Download the latest release and run `Arise Tool.exe`. No installation required.

**2. Add source folders**

Click `+ Add a folder` and select one or more folders containing Unity `.bundle` files.

**3. Set the export folder**

Select the destination folder where extracted assets will be saved.

**4. Start extraction**

Click `Start`. The application will automatically:

- Scan all source folders for `.bundle` files
- Calculate hashes to detect new or modified bundles
- Extract Texture2D and Sprite assets
- Skip already-processed assets
- Classify and organize exported files into named folders

No further interaction is required.

---

## Recommended Bundle Locations

The following folders are found inside the Solo Leveling: ARISE installation directory:

| Folder | Content |
|---|---|
| `backdownload_assets_assets\assetbundles\ui` | Character portraits, banners, interface elements |
| `predownload_assets_assets\assetbundles\ui` | Additional predownloaded UI assets |
| `backdownload_assets_assets\assetbundles\production` | Story assets, events, gameplay textures |
| `backdownload_assets_assets\assetbundles\bg` | Backgrounds, loading screens, environment textures |

---

## How It Works

Arise Tool scans Unity bundles and computes SHA256 hashes to track their state across runs.

On each extraction session:

1. All `.bundle` files in the source folders are scanned
2. Each bundle hash is compared against the local SQLite database
3. New and modified bundles are queued for extraction
4. Assets are extracted using AssetStudioModCLI
5. Each asset is classified by name and placed into the correct output folder
6. The database is updated to prevent future duplicates

Unchanged bundles are skipped entirely. Only new or updated content is processed.

---

## Classification System

Assets are automatically sorted into over 40 categories, including:

- Named character folders (with sub-folders for skills and skins)
- Boss and mob folders
- Event folders (anniversaries, seasonal events)
- UI, weapons, items, gacha, shop, and more

The classification system is driven by a `rules.json` file that can be extended without modifying the application. New characters can also be added directly from the interface.

---

## What's Included

| Feature | Status |
|---|---|
| Texture2D extraction | Included |
| Sprite extraction | Included |
| PNG export | Included |
| Incremental extraction | Included |
| Automatic classification | Included |
| SQLite database | Included |
| Bundle hash detection | Included |
| Extraction logs | Included |
| Multi-folder support | Included |
| Real-time progress tracking | Included |
| Asset browser with filters | Included |
| Persistent settings | Included |

---

## Planned

- WebP export option
- Custom classification rules editor in the UI
- Advanced search and filtering
- Batch asset operations
- Export statistics and reports

---

## Bug Reports and Feedback

If you encounter a bug or want to suggest a feature, open an Issue on this repository.

All feedback is welcome.

---

## Copyright Notice

Solo Leveling: ARISE and all related assets, characters, names and properties are the intellectual property of their respective owners.

Arise Tool is an independent fan-made utility intended for personal and educational use only.

This repository does not distribute any copyrighted game assets. The software extracts assets from a locally installed copy of the game that the user owns.

The source code of Arise Tool may be used, modified and distributed in accordance with the project license.