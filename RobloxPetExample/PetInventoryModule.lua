local PetInventoryModule = {}
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")

local inventoryStore = DataStoreService:GetDataStore("PetInventory")

-- Player data cache
local playerData = {}

local function loadData(player)
    local success, data = pcall(function()
        return inventoryStore:GetAsync(player.UserId)
    end)
    if success and type(data) == "table" then
        playerData[player.UserId] = data
    else
        playerData[player.UserId] = {
            Inventory = {},
            EquippedPet = nil,
            Favorites = {}
        }
    end
end

local function saveData(player)
    local data = playerData[player.UserId]
    if not data then return end
    pcall(function()
        inventoryStore:SetAsync(player.UserId, data)
    end)
end

function PetInventoryModule.AddPet(player, petName)
    local data = playerData[player.UserId]
    table.insert(data.Inventory, petName)
end

function PetInventoryModule.EquipPet(player, petName)
    local data = playerData[player.UserId]
    data.EquippedPet = petName
end

function PetInventoryModule.UnequipPet(player)
    local data = playerData[player.UserId]
    data.EquippedPet = nil
end

function PetInventoryModule.ToggleFavorite(player, petName)
    local data = playerData[player.UserId]
    if data.Favorites[petName] then
        data.Favorites[petName] = nil
    else
        data.Favorites[petName] = true
    end
end

function PetInventoryModule.GetPlayerData(player)
    return playerData[player.UserId]
end

Players.PlayerAdded:Connect(loadData)
Players.PlayerRemoving:Connect(function(player)
    saveData(player)
    playerData[player.UserId] = nil
end)

return PetInventoryModule
