# Roblox Pet System Example

This folder contains a simple example of a pet inventory system for Roblox. The code is split into several scripts that should be placed in specific services in your game.

## Files

- **PetDataModule.lua** – stores information about available pets. Place this ModuleScript in `ReplicatedStorage`.
- **PetInventoryModule.lua** – handles inventory data and saving using DataStore. Place this ModuleScript in `ServerScriptService`.
- **ServerInit.lua** – creates RemoteEvents and connects them to `PetInventoryModule`. This should be a Script inside `ServerScriptService` next to the inventory module.
- **PetUIController.lua** – client-side LocalScript that displays the inventory UI and sends requests via RemoteEvents. Place this LocalScript inside `StarterPlayerScripts` or a GUI element.

## Required Objects

1. **Folder `PetEvents`** – created by `ServerInit.lua` under `ReplicatedStorage`. It contains:
   - `RequestInventory` (`RemoteFunction`)
   - `EquipPet` (`RemoteEvent`)
   - `ToggleFavorite` (`RemoteEvent`)

2. **DataStoreService** – used by `PetInventoryModule.lua` to save and load player data. No additional setup is required beyond enabling API access in Roblox Studio settings if you want to test saving locally.

3. **UI** – `PetUIController.lua` dynamically creates a simple inventory window. You can style it further by editing the script or replacing it with your own UI elements.

## How It Works

- When the server starts, `ServerInit.lua` creates the remote events and listens for inventory actions from clients.
- When a player joins, `PetInventoryModule` loads their saved data (inventory, equipped pet, favorites). When they leave, the data is saved.

- If a player has no saved data, they begin with three starter pets: **Cat**, **Dog**, and **Fox**.

- The client requests its inventory via the `RequestInventory` RemoteFunction and renders buttons for each pet. Clicking a pet button equips or unequips it. Right-clicking toggles the favorite status.

This is a minimal setup intended for learning purposes. In a real game you might expand it with animations, more sophisticated data handling and additional gameplay logic.
