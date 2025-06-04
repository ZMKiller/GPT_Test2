# Roblox Pet System Example

This folder contains a simple example of a pet inventory system for Roblox. The code is split into several scripts that should be placed in specific services in your game.

## Files

- **PetDataModule.lua** – stores information about available pets. Place this ModuleScript in `ReplicatedStorage`.
- **PetInventoryModule.lua** – handles inventory data and saving using DataStore. Place this ModuleScript in `ServerScriptService`.
- **ServerInit.lua** – creates RemoteEvents and connects them to `PetInventoryModule`. This should be a Script inside `ServerScriptService` next to the inventory module.

- **PetUIController.lua** – client-side LocalScript that displays the inventory UI with five equipped slots and item tooltips. Place this LocalScript inside `StarterPlayerScripts` or a GUI element.


## Required Objects

1. **Folder `PetEvents`** – created by `ServerInit.lua` under `ReplicatedStorage`. It contains:
   - `RequestInventory` (`RemoteFunction`)

   - `EquipPet` (`RemoteEvent`) – first argument is the action (`"equip"` or `"unequip"`), second is the pet name or slot index

   - `ToggleFavorite` (`RemoteEvent`)

2. **DataStoreService** – used by `PetInventoryModule.lua` to save and load player data. No additional setup is required beyond enabling API access in Roblox Studio settings if you want to test saving locally.

3. **UI** – `PetUIController.lua` dynamically creates a simple inventory window. You can style it further by editing the script or replacing it with your own UI elements.

## How It Works

- When the server starts, `ServerInit.lua` creates the remote events and listens for inventory actions from clients.

- When a player joins, `PetInventoryModule` loads their saved data (inventory with counts, equipped pets list and favorites). Data is saved when the player leaves.
- If a player has no saved data, they begin with three starter pets.
- The client requests its inventory via `RequestInventory` and shows a grid of pet icons. Identical pets are stacked and show a count. Five equipped slots are displayed above the inventory.
- Hovering a pet icon shows a tooltip with the pet's name, strength and rarity following the mouse. Clicking an inventory icon equips one copy of the pet; clicking an equipped slot unequips that pet.


This is a minimal setup intended for learning purposes. In a real game you might expand it with animations, more sophisticated data handling and additional gameplay logic.
