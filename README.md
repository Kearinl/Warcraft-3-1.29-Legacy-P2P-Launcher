# Warcraft 3 1.29 Legacy P2P Launcher

A lightweight peer-to-peer launcher designed to connect players to **Warcraft III Legacy 1.29** sessions without relying on traditional matchmaking services.

This project preserves and enhances the classic Warcraft III 1.29 experience by enabling **direct LAN-like connectivity over the internet** using a decentralized P2P networking layer and LAN packet tunneling system.

---

## 📌 Overview

This system extends Warcraft III LAN multiplayer beyond local networks by emulating LAN traffic across the internet.

Instead of modifying the game, it operates at the **network packet layer**, capturing LAN broadcasts and rebroadcasting them across connected peers.

The Unity launcher acts as a **packet bridge + session manager**, allowing remote players to appear as if they are on the same local network.

---

## 🧠 Core Concept

Warcraft III LAN uses UDP broadcast packets (port **6112**) to discover games.

This system:

- Captures LAN broadcast packets locally
- Sends them to a master server / peer network
- Rebroadcasts them remotely as LAN traffic
- Tricks Warcraft III into detecting remote games as local LAN sessions

---

## 🏗️ System Architecture


Warcraft III 1.29 Client
↓ UDP 6112 (LAN Broadcast)
Npcap Packet Capture Layer
↓
SharpPcap + PacketDotNet Parser
↓
Unity Launcher (Packet Bridge)
↓
Master Server (Session Discovery API)
↓
P2P UDP Tunnel Network
↓
Remote Unity Launcher
↓
Packet Injection (LAN Re-broadcast)
↓
Remote Warcraft III Client
↓
LAN Game Appears in Menu


---

## ⚙️ System Components

### 🖥️ Client (Player Side)

Each player runs:

- Warcraft III 1.29 Legacy Client
- Unity Launcher (P2P Bridge Application)
- Npcap Packet Driver

### Required Libraries:
- SharpPcap.dll (packet capture)
- PacketDotNet.dll (packet parsing)
- Unity Networking (UDP/TCP relay system)

---

### 🌐 Master Server

Built using **ASP.NET Core 8**

Responsibilities:
- Track active game sessions
- Handle host registration
- Manage peer discovery
- Provide session metadata (IP, ports, IDs)

⚠️ The master server does NOT handle gameplay traffic.

---

### 🔌 P2P / UDP Tunnel Layer

Handles real-time packet forwarding:

- Warcraft III LAN discovery packets
- Session broadcast replication
- Peer-to-peer communication
- Optional relay fallback for strict NAT networks

---

## 🔧 Requirements

### Client Requirements
- Warcraft III 1.29 Legacy Client installed
- Windows OS
- Unity Launcher build
- Npcap installed (packet capture driver)

### Server Requirements
- ASP.NET Core 8 runtime
- Open TCP port: `5047`
- Open UDP ports:
  - `6112` (Warcraft LAN)
  - `6200` (P2P relay)

---

## 🌍 Master Server Setup

### Make Server Public

Change:

```
app.Run();

To:

app.Run("http://0.0.0.0:5047");

This allows external connections from clients.
```

### 🔓 Port Forwarding
```
Port	Protocol	Purpose
5047	TCP	Master Server API
6112	UDP	Warcraft III LAN Traffic
6200	UDP	Peer-to-peer relay
```

---

### 🧩 Unity Launcher Setup

Inside MasterServerClient.cs:
```
Local Development:
public string baseUrl = "http://localhost:5047";

Production:
public string baseUrl = "http://YOUR_PUBLIC_IP:5047";
```
---

### 🎮 How It Works

Hosting a Game:
```
Launch Unity Launcher
   ↓
Click "Host Game"
   ↓
Start Warcraft III 1.29
   ↓
Open LAN menu
   ↓
Game broadcasts LAN packets
   ↓
Launcher captures and forwards packets
```
Joining a Game:
```
Open Unity Launcher
   ↓
Connect to Master Server
   ↓
Browse active sessions
   ↓
Select a host
   ↓
Launcher connects via UDP tunnel
   ↓
Warcraft III detects LAN game
   ↓
Join normally
```

🔄 Network Flow:
```
Host Warcraft III
   ↓
LAN Broadcast (UDP 6112)
   ↓
Npcap Capture
   ↓
Unity Launcher
   ↓
Master Server / Peer Network
   ↓
Remote Launcher
   ↓
LAN Packet Injection
   ↓
Remote Warcraft III detects game
```
---

### ⚠️ Important Notes:

This system does NOT modify Warcraft III itself.

Warcraft III still handles:
```
Game simulation
Unit control & sync
Map loading
Multiplayer logic
```
Your system only handles:
```
LAN packet emulation
Peer discovery
UDP forwarding
Session coordination
```
### 🧪 Limitations:
```
NAT restrictions may block direct P2P
Firewall must allow UDP traffic
LAN discovery depends on packet timing accuracy
No encryption layer in base version
Some routers may block broadcast replication
```
### 🚀 Future Improvements:
```
NAT hole punching (STUN-style system)
Encrypted UDP tunnels
Dedicated relay servers
Regional matchmaking nodes
Anti-desync validation system
Auto-reconnect on packet loss
Spectator mode over P2P
```
### 🧠 Design Philosophy:
```
Preserve Warcraft III 1.29 original gameplay
Avoid modifying game binaries
Emulate LAN instead of replacing networking
Keep system lightweight and modular
Enable community-hosted multiplayer revival
```
### 📜 Disclaimer:
```
This project is not affiliated with Blizzard Entertainment.

Warcraft III is a trademark of Blizzard Entertainment.
```
---
