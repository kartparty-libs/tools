# 🛠️ Game Operations Toolkit

A CLI-driven utility suite for game designers, operators, and GMs—designed to streamline configuration, data management, and live game control.

## 🔧 Core Functions

### 📊 Data & Schema Management  
- Auto-generate, validate, and migrate relational database schemas (PostgreSQL) from YAML/JSON definitions  
- Sync in-game static tables (e.g., kart stats, item effects, track parameters) with version-controlled configs  
- Export/import runtime data snapshots (e.g., player inventories, active events) for backup or staging  

### ⚙️ Configuration Engine  
- Unified config layer supporting environment-aware overrides (dev/staging/prod)  
- Hot-reload support: push updated balance rules, drop rates, or season settings without server restart  
- Type-safe validation + diff preview before applying changes  

### 🎮 Game Design Studio  
- Interactive CLI wizard to define new karts, items, tracks, and race modes  
- Preview impact of stat changes (e.g., “How does +10% boost duration affect lap time?”) using built-in simulators  
- Generate localized strings, asset manifests, and client-side bundles  

### 🪄 GM Console  
- Secure, role-based terminal interface for live operations:  
  - Spawn items, teleport players, adjust scores, or freeze sessions  
  - Broadcast announcements, trigger events, or rollback misbehaving races  
  - Query & inspect player/NFT state across services in real time  

## ✅ Principles  
- **Safe by default**: All destructive or state-altering commands require confirmation + audit log  
- **No magic**: Every action maps transparently to underlying data or API calls  
- **Extensible**: Plugin system supports custom validators, exporters, and GM commands  

Run `./toolkit --help` to get started. Configs live in `/configs`; logs and audits are auto-archived.