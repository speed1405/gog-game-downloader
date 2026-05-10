# GOG Offline Backup Downloader — Technical & Design Document

## 1) Modern UI Framework & Styling

### Recommended stack
- **Primary:** C# + **Avalonia UI** + **SukiUI**
- **Fallback:** Avalonia Fluent theme if SukiUI styling depth is not needed

Why this stack:
- Cross-platform desktop support (Windows, macOS, Linux)
- Modern visual language support (blurred surfaces, glass cards, animated states)
- Strong AXAML styling and transition support for CSS-like interactions
- MVVM-friendly architecture for scalability

### Mica/Acrylic strategy
- On **Windows 11**: apply system backdrop via interop (`DwmSetWindowAttribute`, `DWMWA_SYSTEMBACKDROP_TYPE`) for Mica/Acrylic-like host effects.
- On **non-Windows**: emulate glass using blurred translucent containers:
  - Overlay opacity: `0.85`
  - Blur radius: `24`
  - Subtle border highlight for depth

### Dark-mode-first palette

| Role | Token | Hex |
|---|---|---|
| Background (base) | `--bg-base` | `#0D0F14` |
| Surface (cards) | `--bg-surface` | `#161A23` |
| Surface elevated | `--bg-elevated` | `#1E2330` |
| Border subtle | `--border` | `#2A2F3D` |
| Text primary | `--text-primary` | `#E8EAF0` |
| Text secondary | `--text-secondary` | `#8B91A7` |
| Accent (brand) | `--accent` | `#6C63FF` |
| Downloading | `--status-dl` | `#4FC3F7` |
| Verified | `--status-ok` | `#69F0AE` |
| Update Ready | `--status-update` | `#FFD740` |
| Error | `--status-err` | `#FF5370` |
| Queued | `--status-queue` | `#B0BEC5` |

Status presentation rules:
- Use a **3px left accent stripe** on cards
- Include **dot/icon + text label** (not color-only encoding)
- Reuse status colors for progress rings and badges

### Shape and motion
- Card corner radius: `12`
- Button corner radius: `8`
- Pill badge corner radius: `20`
- Hover scale: `1.00 -> 1.02` over `150ms`
- Opacity/color transitions: `150ms` default duration

---

## 2) Navigation & Layout

### Sidebar navigation
- Collapsible side pane with two states:
  - Expanded width: `220`
  - Collapsed width: `60`
- Main entries:
  - Library
  - Downloads
  - Storage
  - Settings
- Active item treatment:
  - Left accent bar (3px, brand color)
  - Tinted background (`rgba(108,99,255,0.12)`)

Implementation:
- Avalonia `SplitView` with `IsPaneOpen` binding
- Bottom chevron for expand/collapse
- Persistent navigation state in app settings

### Responsive game grid
- Container: virtualized scroller + grid layout
- Card size target: `180x260`
- Gap: `12`
- Responsive columns:
  - Width > 1600: 7 columns
  - Width > 1200: 5 columns
  - Width < 900: 3 columns

Card composition:
- Poster area with rounded top corners
- Download progress strip (conditional)
- Footer metadata (title, status, size)

Hover interaction:
- Fade-in overlay (`rgba(0,0,0,0.55)`)
- Quick actions:
  - Download Now
  - View Metadata
- Lift shadow + scale animation on hover

---

## 3) Performance-Centric UX

### Image virtualization and lazy loading
Objective: maintain smooth scrolling with hundreds of titles at 60fps.

Design:
1. Use a virtualized items control (`ItemsRepeater` + grid layout).
2. `GameCardViewModel` starts with `Poster = null`.
3. Bind image source to poster with placeholder fallback.
4. Subscribe to scroll changes and queue visible-range image loads.
5. Limit parallel image decode/network with `SemaphoreSlim(4)`.
6. Memory cache with weak references to avoid memory bloat.
7. Disk cache for poster reuse across sessions (`poster-cache` folder).

Expected behavior:
- Low startup overhead
- Minimal UI thread blocking
- Stable memory usage while scrolling long libraries

### Glassmorphism progress overlay
- Floating bottom-right notification stack
- Glass style:
  - translucent dark background
  - blur backdrop
  - rounded corners (`16`)
  - subtle border highlight
- Animations:
  - enter: slide-up + fade (`~250ms`)
  - dismiss: fade/slide-out
- Notification policy:
  - Success: auto-dismiss (4s)
  - Error: persistent with retry action

---

## 4) Storage & Drive Management UI

### Drive map in Settings > Storage
Visual model:
- Drive cards with segmented usage bars
- Labels for used/free and role assignment
- Optional network/mount locations

Data source:
- `DriveInfo.GetDrives()` at startup and when opening storage settings
- Periodic refresh every 30s for removable media changes

Bar segment semantics:
- Blue: GOG backup data
- Purple: other used space
- Gray: free space

Actions:
- Mark drive as **Primary** (star toggle)
- Mark drive as **Backup Target**
- Add network path/mount point

Warnings:
- <15% free: amber warning state
- <5% free: red critical state

Persistence:
- Selected primary/backup paths stored in local DB settings table

---

## 5) Advanced Interaction

### System tray integration
- Tray icon with dynamic mini progress ring
- Ring redraw on aggregate download progress changes
- Context menu:
  - Resume All
  - Pause All
  - Active job summaries
  - Open Settings
  - Exit

Implementation notes:
- Use an Avalonia tray package and platform-specific icon rendering bridge where needed.
- Render a `32x32` icon bitmap for progress arc updates.

### Native toast notifications
- Windows: `Microsoft.Toolkit.Uwp.Notifications`
- Linux: D-Bus notifications (`libnotify`/`Tmds.DBus`)
- macOS: native notification center bridge

Abstraction:
- `INotificationService` interface
- Platform-specific implementations wired through DI

Use cases:
- Download completed
- Verification complete
- Download failed with quick action

---

## 6) Technical Backend

### Local database strategy
- **Preferred DB:** SQLite + EF Core
- Rationale:
  - Reliable local relational storage
  - Strong tooling and query ergonomics
  - WAL mode supports concurrent read/write patterns

### Core schema

```sql
Games          (Id TEXT PK, Title, Slug, ReleaseYear, GenreJson, PosterUrl, BackgroundUrl, UpdatedAt)
GameVersions   (Id INT PK, GameId FK, Version, BuildId, Size, OS, Language, InstallerUrl, ChecksumMd5)
DownloadJobs   (Id INT PK, GameVersionId FK, TargetPath, Status, BytesTotal, BytesDone, StartedAt, FinishedAt, ErrorMessage)
DownloadChunks (Id INT PK, JobId FK, ChunkIndex, ByteOffset, ByteLength, ChecksumMd5, IsVerified)
AppSettings    (Key TEXT PK, Value TEXT)
```

### Startup and caching model
1. First run: ingest game catalog from GOG API in background.
2. Subsequent runs: read from SQLite for near-instant library display.
3. Use no-tracking read queries for UI-heavy list rendering.
4. Bind virtualized list immediately; hydrate posters lazily.
5. Run migrations at startup (`MigrateAsync`) safely.
6. Keep DB in per-user app-data location by platform.

### Download resume and integrity
- Chunk-aware downloads with byte-range resume
- Persist chunk verification status
- Skip verified chunks on retry/resume
- Store checksum and error details for diagnostics

### Retention policy
- Metadata refreshed asynchronously when stale (>24h)
- Poster cache retained until invalidated by source change
- Download history retained; optional cleanup setting for old records

---

## 7) Architecture & Dependency Injection

Pattern:
- MVVM + DI (`Microsoft.Extensions.DependencyInjection`)

Primary interfaces:
- `IGameRepository` -> `SqliteGameRepository`
- `IDownloadService` -> `GogDownloadService`
- `INotificationService` -> platform implementations
- `IStorageService` -> drive + path management
- `IImageCacheService` -> memory + disk poster cache

Benefits:
- Testable view models
- Platform abstraction boundaries
- Clear separation of UI, domain, and infrastructure

---

## 8) Delivery Phases (Suggested)

1. **Foundation**
   - Avalonia shell, theme tokens, sidebar, base pages
2. **Library UX**
   - Virtualized cards, lazy posters, hover actions
3. **Downloads**
   - Queue model, progress overlays, status badges
4. **Storage**
   - Drive map UI, primary/backup assignment, thresholds
5. **System Integration**
   - Tray icon, toast notifications, quick actions
6. **Data Layer**
   - SQLite schema, migrations, metadata cache, history

This phased order delivers visible UX early while reducing architecture risk.
