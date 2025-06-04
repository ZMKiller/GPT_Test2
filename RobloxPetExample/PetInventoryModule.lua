local PetInventoryModule = {}
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local PetDataModule = require(ReplicatedStorage:WaitForChild("PetDataModule"))

local DEFAULT_PETS = {}
for petName in pairs(PetDataModule.Pets) do
    if #DEFAULT_PETS >= 3 then break end
    table.insert(DEFAULT_PETS, petName)
end


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

            Inventory = table.clone(DEFAULT_PETS),

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


-- Returns player data, creating default tables if needed.
function PetInventoryModule.GetPlayerData(player)
    local data = playerData[player.UserId]
    if not data then
        data = {
            Inventory = table.clone(DEFAULT_PETS),
            EquippedPet = nil,
            Favorites = {}
        }
        playerData[player.UserId] = data
    end
    return data

end

Players.PlayerAdded:Connect(loadData)
Players.PlayerRemoving:Connect(function(player)
    saveData(player)
    playerData[player.UserId] = nil
end)

return PetInventoryModule
