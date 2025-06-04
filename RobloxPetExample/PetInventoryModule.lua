local PetInventoryModule = {}
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local PetDataModule = require(ReplicatedStorage:WaitForChild("PetDataModule"))

local DEFAULT_PETS = {}
local added = 0
for petName in pairs(PetDataModule.Pets) do
    if added >= 3 then break end
    DEFAULT_PETS[petName] = 1
    added += 1
end

local inventoryStore = DataStoreService:GetDataStore("PetInventory")

-- Player data cache
local playerData = {}

local function ensureFormat(data)
    if type(data.Inventory) == "table" then
        -- convert array inventory to count table if necessary
        if #data.Inventory > 0 then
            local counts = {}
            for _, name in ipairs(data.Inventory) do
                counts[name] = (counts[name] or 0) + 1
            end
            data.Inventory = counts
        end
    else
        data.Inventory = table.clone(DEFAULT_PETS)
    end

    if type(data.EquippedPets) ~= "table" then
        data.EquippedPets = {}
    end
    if type(data.Favorites) ~= "table" then
        data.Favorites = {}
    end
end

local function loadData(player)
    local success, data = pcall(function()
        return inventoryStore:GetAsync(player.UserId)
    end)
    if success and type(data) == "table" then
        ensureFormat(data)
        playerData[player.UserId] = data
    else
        playerData[player.UserId] = {
            Inventory = table.clone(DEFAULT_PETS),
            EquippedPets = {},
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
    local data = PetInventoryModule.GetPlayerData(player)
    data.Inventory[petName] = (data.Inventory[petName] or 0) + 1
end

function PetInventoryModule.EquipPet(player, petName)
    local data = PetInventoryModule.GetPlayerData(player)
    if (data.Inventory[petName] or 0) <= 0 then return end
    if #data.EquippedPets >= 5 then return end
    table.insert(data.EquippedPets, petName)
    data.Inventory[petName] = data.Inventory[petName] - 1
    if data.Inventory[petName] <= 0 then
        data.Inventory[petName] = nil
    end
end

function PetInventoryModule.UnequipPet(player, slotIndex)
    local data = PetInventoryModule.GetPlayerData(player)
    local petName = data.EquippedPets[slotIndex]
    if not petName then return end
    table.remove(data.EquippedPets, slotIndex)
    data.Inventory[petName] = (data.Inventory[petName] or 0) + 1
end

function PetInventoryModule.ToggleFavorite(player, petName)
    local data = PetInventoryModule.GetPlayerData(player)
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
            EquippedPets = {},
            Favorites = {}
        }
        playerData[player.UserId] = data
    end
    ensureFormat(data)
    return data
end

Players.PlayerAdded:Connect(loadData)
Players.PlayerRemoving:Connect(function(player)
    saveData(player)
    playerData[player.UserId] = nil
end)

return PetInventoryModule
