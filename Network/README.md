# Network

**Improves Valheim multiplayer networking, especially on larger or busier servers.**

Install it and leave the default settings alone. The defaults are recommended for most players and servers.

## What does it do?

As more players join a normal Valheim server, the game takes longer and longer to send each player updates about what is happening around them.

This can contribute to things like:

* Players or creatures appearing to update slowly
* Ships feeling delayed or out of sync
* Doors, dropped items, and other objects taking longer to update
* Player markers on the map looking choppy
* Busy areas feeling less responsive as more people join

Network changes how Valheim handles these updates so every player gets them more regularly instead of updates becoming increasingly spread out as the server fills up.

## Benefits

* **More consistent multiplayer updates** as more players join.
* **Fresher nearby players, creatures, ships, doors, items, and other world objects.**
* **Important moving things are updated first**, such as players, ships, and creatures.
* **Better use of Steam networking** instead of Valheim's unusually low send limit.
* **Smoother public player markers** when Network is installed on both the server and client.
* Works without modifying Valheim's game assemblies.
* No special configuration required for normal use.

Network helps with stale or delayed world updates. It cannot fix bad routing, packet loss, slow hardware, or insufficient upload bandwidth.

## How much does it change?

The difference becomes much more noticeable as player count increases.

Assuming the server can process 30 update-loop cycles per second:

| Players | Vanilla per player | Network per player |
| ------: | -----------------: | -----------------: |
|       5 |           ~5.0/sec |            ~30/sec |
|      10 |           ~2.7/sec |            ~30/sec |
|  **15** |       **~1.9/sec** |        **~20/sec** |
|      20 |           ~1.4/sec |            ~20/sec |

In plain English:

**Vanilla gives each player fewer opportunities to receive world updates as more people join. Network keeps those opportunities much more consistent.**

This does **not** mean every object is sent 20-30 times every second.

Each pass is simply an opportunity for Valheim to send a chunk of changed world information to that player. If nothing relevant changed, that chunk may be small or empty.

## What does it cost?

Network allows Valheim to do more networking work than it normally does.

On busy servers, that can mean somewhat higher:

* CPU usage
* Upload bandwidth

The work is spread across update-loop cycles instead of being dumped into a single frame.

For most servers, just use the defaults.

If you're running on weaker hardware or limited upload bandwidth, there is a **Balanced Bandwidth** preset in the troubleshooting section below.

## Where should I install it?

| Install Network on              | What it improves                                                                      |
| ------------------------------- | ------------------------------------------------------------------------------------- |
| **Dedicated server only**       | Server-to-player updates, nearby-object selection, and send priority for every player |
| **Player hosting a world only** | The same server-side improvements for that hosted world                               |
| **Client only**                 | That client's outgoing updates and Steam connection                                   |
| **Server/host + clients**       | Everything, including smooth player markers                                           |

### Do all players need it?

No.

Unmodded clients can still join a server using Network.

Network does not enforce versions or kick players who do not have it installed.

For the largest benefit, install it on the **dedicated server or player hosting the world**.

Clients can also install it for the remaining client-side improvements. Smooth public player markers require Network on both the server and client.

## Installation

### Gale Mod Manager

Gale is the recommended mod manager.

Get it from:

* [Hexium](https://hexium.gg/mod-manager)
* [GitHub](https://github.com/Kesomannen/gale)
* [Thunderstore](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/)

Gale supports both Hexium and Thunderstore mods in the same place.

Select Valheim, install Network into the profile you use to launch the game, and start Valheim through Gale.

Dedicated servers still need Network installed in the server's own `BepInEx/plugins` folder. Installing it only in your personal Gale profile does not install it on the dedicated server.

### Manual Installation

1. Install BepInEx in Valheim's root folder.
2. Put Network in `BepInEx/plugins`.
3. Start the game or server once.

That's it.

The configuration file is created automatically at:

```text
BepInEx/config/org.bepinex.plugins.network.cfg
```

**You do not need to change it.** The default settings are intended for normal use.

<details>
<summary><strong>How Network changes Valheim</strong></summary>

### Fairer world updates

Vanilla Valheim waits `0.05` seconds, then handles one connection (`peer`) per update-loop cycle until everyone gets a turn.

The problem is that every additional player makes the wait until someone's next turn longer.

Network replaces this with an evenly paced adaptive round-robin scheduler:

* **1-10 players:** targets 30 send passes/sec each.
* **11-14 players:** smoothly decreases from 30 to 20.
* **15+ players:** targets 20 send passes/sec each.
* Each player is handled at most once per update-loop cycle.
* Network cannot run faster than the server's update loop.

Each send pass allows Network to send one chunk (`ZDO batch`) of changed world data to one player.

If little or nothing has changed, that batch can be small or empty.

### Why does vanilla slow down?

Using a server running at 30 update-loop cycles/sec:

| Players | Vanilla per player | Network per player |
| ------: | -----------------: | -----------------: |
|       5 |            5.0/sec |           30.0/sec |
|      10 |            2.7/sec |           30.0/sec |
|  **15** |        **1.9/sec** |       **20.0/sec** |
|      20 |            1.4/sec |           20.0/sec |

A headless dedicated server still runs Valheim's update loop even though it does not render graphics.

The `30/sec` example above refers to **server processing speed**, not your monitor refresh rate or graphical FPS.

### Better nearby-object checks

Valheim normally updates a player's server-side reference position every two seconds.

That reference position is used when deciding which nearby world objects should be sent to that player.

Network instead reads the player's live character ZDO position before building the send list.

In other words, it checks around **where you are now**, rather than potentially checking around where you were up to two seconds ago.

### Moving things first

Network gives additional send priority to important moving objects.

The priority order is:

1. Players
2. Ships
3. Creatures

Valheim's normal ownership, distance, and staleness rules still participate in the final decision.

Network does not permanently starve scenery or override Valheim's normal networking rules. Older world state still gets its turn to catch up.

### Smarter Steam settings

Network removes Valheim's low Steam send limit while still allowing Steam Networking Sockets to reduce its rate when a connection is struggling.

The default pending-data buffer is **4 MB**.

This gives the connection room to absorb temporary bursts without allowing an enormous queue of old reliable updates to build up during congestion.

Network also uses separate configurable:

* Connection timeout
* Send rate ceiling
* Send rate floor
* Send buffer size

Raising these values does not automatically make a bad connection faster.

### Crossplay / PlayFab

Network works on crossplay servers.

When the connection uses PlayFab, the Steam-specific connection settings are skipped because that connection is not using Steam Networking Sockets.

The rest of Network's improvements still apply.

### Smooth public map markers

Normally, remote public player positions can appear to jump between updates on the map.

When enabled:

* The server sends public player positions every `0.5` seconds.
* Network clients smoothly interpolate movement between those positions.
* Private player positions remain private.
* Large jumps such as teleports snap immediately instead of sliding across the map.

This requires Network on both the server/host and the client viewing the markers.

### Early connection guard

Network also includes protection for world data (`ZDOData`) arriving unusually early during connection setup.

It keeps early data separate for each connection until that client's ZDO peer exists.

This catches data that arrives before `ZDOMan.AddPeer` has registered Valheim's normal handler, which can happen when another mod changes connection timing.

</details>

<details>
<summary><strong>Presets and troubleshooting</strong></summary>

## Classic Network

This disables the newer networking improvements and leaves only Network's original behavior active:

* Steam send-limit removal
* Early `ZDOData` connection guard

The following are disabled:

* Adaptive scheduling
* Live peer positions
* Larger ZDO batches
* Actor priority
* Improved Steam settings
* Smooth map markers
* Patch conflict warnings

The connection timeout remains vanilla.

```ini
[1 - General]
Enable Networking Improvements = Off
```

## Balanced Bandwidth

Use this if the server or host has limited upload bandwidth or starts dropping packets in busy areas.

```ini
[2 - ZDO Sending]
Send Interval = 0.1
Increase Batch Size = Off
```

This targets approximately **10 send passes/sec per player** with vanilla-sized batches.

At 15 players and 30 server update-loop cycles/sec, that is still more than five times vanilla's per-player send rate.

If the server still struggles, you can also set:

```ini
Max Peers Per Frame
```

to approximately half the active player count.

When testing changes, compare performance with:

* The same number of players
* In the same area
* Under approximately the same server load

Otherwise, the comparison may not mean much.

## Keep better selection and priority, but use vanilla scheduling

If you want Network's live position checks and send priority without its adaptive scheduler:

```ini
[2 - ZDO Sending]
Adaptive ZDO Scheduler = Off
Increase Batch Size = Off
Refresh Peer Interest Position = On

[3 - Send Priority]
Prioritize Players And Creatures = On
```

</details>

<details>
<summary><strong>Full config reference</strong></summary>

Config values are **not synchronized**.

Change them separately on each server, host, or client where Network is installed.

The tables below use these labels:

* **Server/Host** - Dedicated server or player hosting the world.
* **Client** - Each player's game.
* **This installation** - Applies locally wherever Network is installed.

Every optional feature below also requires:

```ini
Enable Networking Improvements = On
```

### 1 - General

| Setting                          | Default | Where             | What to know                                                                                                                               |
| -------------------------------- | ------: | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `Enable Networking Improvements` |    `On` | This installation | Master switch for the added features. Turning it off leaves only the Steam send-limit removal and early `ZDOData` connection guard active. |
| `Report Patch Conflicts`         |    `On` | This installation | Logs when other mods patch the same networking methods. This is only a warning and does not automatically mean the other mod is broken.    |

### 2 - ZDO Sending

A **ZDO** is one of Valheim's internal representations of saved/networked world state.

For example:

* Player
* Creature
* Ship
* Door
* Dropped item
* Other synchronized world objects

| Setting                          |       Default | Where             | What to know                                                                                                                                                                                           |
| -------------------------------- | ------------: | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Adaptive ZDO Scheduler`         |          `On` | This installation | Controls which peer gets each send turn using Network's round-robin scheduler. Off restores vanilla timing.                                                                                            |
| `Send Interval`                  |    `0.05` sec | This installation | Sets the base per-player rate. `0.1` targets 10/sec for 15+ players. Smaller sessions can run up to 50% faster. Lower values require more work and upload. Range: `0.01-0.2`.                          |
| `Max Peers Per Frame`            |           `0` | This installation | `0` adds no additional cap. Lower values can reduce stutters but also reduce how frequently players receive updates. Range: `0-128`.                                                                   |
| `Increase Batch Size`            |          `On` | This installation | Enables the configured `Batch Size`. Off restores vanilla's `10,240` byte limit.                                                                                                                       |
| `Batch Size`                     | `20480` bytes | This installation | Maximum world-data chunk per send pass. Larger values can clear queued changes faster but create larger bursts. Do not increase this without measuring an actual queue problem. Range: `10240-262144`. |
| `Refresh Peer Interest Position` |          `On` | Server/Host       | Uses the player's live character position when deciding which nearby world state should be sent.                                                                                                       |

`Send Interval` and `Max Peers Per Frame` require the adaptive scheduler.

`Batch Size` requires `Increase Batch Size`.

### 3 - Send Priority

| Setting                            | Default | Where       | What to know                                                                                                                             |
| ---------------------------------- | ------: | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `Prioritize Players And Creatures` |    `On` | Server/Host | Gives players the largest priority boost, followed by ships and creatures. Vanilla ownership, distance, and staleness rules still apply. |

### 4 - Steam

| Setting                       |         Default | Where             | What to know                                                                                                                                                        |
| ----------------------------- | --------------: | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Use Improved Steam Settings` |            `On` | This installation | Uses Network's timeout, rate, and buffer settings below. Off still removes Valheim's Steam send limit, uses a 100 MB client buffer, and leaves the timeout vanilla. |
| `Connection Timeout`          |     `120000` ms | This installation | Gives slower connections more time to finish loading. A genuinely dead connection also takes longer to time out. Range: `30000-600000`.                             |
| `Send Rate Ceiling`           |  `50000000` B/s | This installation | Maximum rate Steam is allowed to choose, not a constant send rate. If configured below the floor, the floor wins. Range: `153600-50000000`.                         |
| `Send Rate Floor`             |    `153600` B/s | This installation | Lowest rate Steam may choose. **Low is good. Raising this does not make a bad connection faster.** Range: `16384-50000000`.                                         |
| `Send Buffer Size`            | `4194304` bytes | This installation | Maximum queued data per connection. Too small can reject bursts. Too large can hold old updates during congestion. Range: `524288-16777216`.                        |

These settings affect **Steam sockets only**.

They do nothing while improved Steam settings are disabled.

### 5 - Map Markers

| Setting                  |   Default | Where             | What to know                                                                                                            |
| ------------------------ | --------: | ----------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `Smooth Player Markers`  |      `On` | This installation | The server sends fresher public player positions and Network clients smooth their movement.                             |
| `Position Send Interval` | `0.5` sec | Server/Host       | Lower values send more marker packets. Leave this alone unless you have measured a reason to change it. Range: `0.1-2`. |
| `Teleport Threshold`     |    `50` m | Client            | Movement larger than this distance snaps immediately instead of being interpolated. Range: `10-500`.                    |

The last two settings do nothing while `Smooth Player Markers` is disabled.

</details>

## Compatibility

Network blocks the following mods because they attempt to control the same networking send loop:

* Valheim Plus (`org.bepinex.plugins.valheim_plus`)
* VBNetTweaks (`VitByr.VBNetTweaks`)

### ReturnToSender

No manual setup is required.

When Network's master switch and adaptive scheduler are enabled:

* Network removes only ReturnToSender's send-timing patch (`ZDOMan.Update` Harmony transpiler).
* Network uses its own scheduling instead.
* ReturnToSender itself remains loaded.

If either Network's master switch or adaptive scheduler is disabled:

* Network leaves ReturnToSender's send timing alone.

The log reports which mod currently controls the send timing.

### Patch conflict warnings

Network also warns when another mod patches the same networking methods.

A warning **does not mean the other mod is broken or incompatible**.

It means both mods touch some of the same networking code.

If you're reporting a Network bug after seeing one of these warnings, reproduce the issue without the conflicting mod first.

---

For questions or comments, find me in the Hexium or Odin Plus Team Discord:

<table width="100%">
  <tr>
    <td align="center">
      <a href="https://hexium.gg">
        <img
          src="https://hexium.gg/assets/Logo.png"
          alt="Hexium"
          width="64"/>
      </a>
    </td>
    <td align="center">
      <a href="https://discord.gg/Pb6bVMnFb2">
        <img
          src="https://i.imgur.com/XXP6HCU.png"
          alt="Odin Plus Discord"
          width="64"/>
      </a>
    </td>
  </tr>
</table>
