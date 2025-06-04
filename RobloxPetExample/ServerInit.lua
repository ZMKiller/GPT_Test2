-- ServerInit.lua
-- Sets up remote events and binds inventory actions

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Players = game:GetService("Players")

local PetInventoryModule = require(script.Parent:WaitForChild("PetInventoryModule"))

local eventsFolder = Instance.new("Folder")
eventsFolder.Name = "PetEvents"
eventsFolder.Parent = ReplicatedStorage

local requestInventory = Instance.new("RemoteFunction")
requestInventory.Name = "RequestInventory"
requestInventory.Parent = eventsFolder

local equipPet = Instance.new("RemoteEvent")
equipPet.Name = "EquipPet"
equipPet.Parent = eventsFolder

local toggleFavorite = Instance.new("RemoteEvent")
toggleFavorite.Name = "ToggleFavorite"
toggleFavorite.Parent = eventsFolder

requestInventory.OnServerInvoke = function(player)
    return PetInventoryModule.GetPlayerData(player)
end

equipPet.OnServerEvent:Connect(function(player, petName)
    if PetInventoryModule.GetPlayerData(player) then
        if PetInventoryModule.GetPlayerData(player).EquippedPet == petName then
            PetInventoryModule.UnequipPet(player)
        else
            PetInventoryModule.EquipPet(player, petName)
        end
    end
end)

toggleFavorite.OnServerEvent:Connect(function(player, petName)
    PetInventoryModule.ToggleFavorite(player, petName)
end)
