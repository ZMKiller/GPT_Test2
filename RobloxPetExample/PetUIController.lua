-- PetUIController.lua
-- Client-side script that creates inventory UI and binds buttons

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local eventsFolder = ReplicatedStorage:WaitForChild("PetEvents")
local requestInventory = eventsFolder:WaitForChild("RequestInventory")
local equipPet = eventsFolder:WaitForChild("EquipPet")
local toggleFavorite = eventsFolder:WaitForChild("ToggleFavorite")

local player = Players.LocalPlayer
local playerGui = player:WaitForChild("PlayerGui")

local inventoryGui = Instance.new("ScreenGui")
inventoryGui.Name = "PetInventoryGui"
inventoryGui.Parent = playerGui

local inventoryFrame = Instance.new("Frame")
inventoryFrame.Size = UDim2.new(0, 300, 0, 400)
inventoryFrame.Position = UDim2.new(0.5, -150, 0.5, -200)
inventoryFrame.BackgroundColor3 = Color3.fromRGB(50, 50, 50)
inventoryFrame.Parent = inventoryGui

local function refreshInventory()
    for _, child in ipairs(inventoryFrame:GetChildren()) do
        if child:IsA("TextButton") or child:IsA("TextLabel") then
            child:Destroy()
        end
    end

    local data = requestInventory:InvokeServer()
    local yOffset = 0
    for _, petName in ipairs(data.Inventory) do
        local button = Instance.new("TextButton")
        button.Size = UDim2.new(1, -10, 0, 30)
        button.Position = UDim2.new(0, 5, 0, yOffset)
        button.Text = petName
        button.BackgroundColor3 = data.EquippedPet == petName and Color3.fromRGB(0,200,0) or Color3.fromRGB(100,100,100)
        button.Parent = inventoryFrame

        button.MouseButton1Click:Connect(function()
            equipPet:FireServer(petName)
            refreshInventory()
        end)

        button.MouseButton2Click:Connect(function()
            toggleFavorite:FireServer(petName)
            refreshInventory()
        end)

        if data.Favorites[petName] then
            local star = Instance.new("TextLabel")
            star.Size = UDim2.new(0, 30, 1, 0)
            star.Position = UDim2.new(1, -35, 0, 0)
            star.Text = "★"
            star.TextColor3 = Color3.fromRGB(255, 215, 0)
            star.BackgroundTransparency = 1
            star.Parent = button
        end

        yOffset = yOffset + 35
    end
end

refreshInventory()
