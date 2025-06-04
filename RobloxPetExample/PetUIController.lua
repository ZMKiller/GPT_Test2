-- PetUIController.lua

-- Client-side script that displays inventory UI with equipped slots and tooltips

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local UserInputService = game:GetService("UserInputService")

local eventsFolder = ReplicatedStorage:WaitForChild("PetEvents")
local requestInventory = eventsFolder:WaitForChild("RequestInventory")
local equipPetEvent = eventsFolder:WaitForChild("EquipPet")
local toggleFavorite = eventsFolder:WaitForChild("ToggleFavorite")

local PetData = require(ReplicatedStorage:WaitForChild("PetDataModule"))

local player = Players.LocalPlayer
local playerGui = player:WaitForChild("PlayerGui")

local screenGui = Instance.new("ScreenGui")
screenGui.Name = "PetInventoryGui"
screenGui.Parent = playerGui

local mainFrame = Instance.new("Frame")
mainFrame.Size = UDim2.new(0, 400, 0, 450)
mainFrame.Position = UDim2.new(0.5, -200, 0.5, -225)
mainFrame.BackgroundColor3 = Color3.fromRGB(50, 50, 50)
mainFrame.Parent = screenGui

local equipFrame = Instance.new("Frame")
equipFrame.Size = UDim2.new(1, -10, 0, 60)
equipFrame.Position = UDim2.new(0, 5, 0, 5)
equipFrame.BackgroundTransparency = 1
equipFrame.Parent = mainFrame

local equipLayout = Instance.new("UIListLayout")
equipLayout.FillDirection = Enum.FillDirection.Horizontal
equipLayout.Padding = UDim.new(0, 5)
equipLayout.Parent = equipFrame

equipLayout.SortOrder = Enum.SortOrder.LayoutOrder

local gridFrame = Instance.new("Frame")
gridFrame.Size = UDim2.new(1, -10, 1, -70)
gridFrame.Position = UDim2.new(0, 5, 0, 65)
gridFrame.BackgroundTransparency = 1
gridFrame.Parent = mainFrame

local gridLayout = Instance.new("UIGridLayout")
gridLayout.CellSize = UDim2.new(0, 60, 0, 60)
gridLayout.CellPadding = UDim.new(0, 5)
gridLayout.SortOrder = Enum.SortOrder.LayoutOrder
gridLayout.Parent = gridFrame

local tooltip = Instance.new("Frame")
tooltip.Size = UDim2.new(0, 150, 0, 70)
tooltip.BackgroundColor3 = Color3.fromRGB(40, 40, 40)
tooltip.BorderColor3 = Color3.new(1, 1, 1)
tooltip.Visible = false
tooltip.Parent = screenGui

local nameLabel = Instance.new("TextLabel")
nameLabel.Position = UDim2.new(0, 5, 0, 5)
nameLabel.Size = UDim2.new(1, -10, 0, 20)
nameLabel.BackgroundTransparency = 1
nameLabel.TextColor3 = Color3.new(1, 1, 1)
nameLabel.TextXAlignment = Enum.TextXAlignment.Left
nameLabel.Font = Enum.Font.SourceSansBold
nameLabel.TextSize = 16
nameLabel.Parent = tooltip

local strengthLabel = Instance.new("TextLabel")
strengthLabel.Position = UDim2.new(0, 5, 0, 25)
strengthLabel.Size = UDim2.new(1, -10, 0, 20)
strengthLabel.BackgroundTransparency = 1
strengthLabel.TextColor3 = Color3.new(1, 1, 1)
strengthLabel.TextXAlignment = Enum.TextXAlignment.Left
strengthLabel.Font = Enum.Font.SourceSans
strengthLabel.TextSize = 14
strengthLabel.Parent = tooltip

local rarityLabel = Instance.new("TextLabel")
rarityLabel.Position = UDim2.new(0, 5, 0, 45)
rarityLabel.Size = UDim2.new(1, -10, 0, 20)
rarityLabel.BackgroundTransparency = 1
rarityLabel.TextColor3 = Color3.new(1, 1, 1)
rarityLabel.TextXAlignment = Enum.TextXAlignment.Left
rarityLabel.Font = Enum.Font.SourceSans
rarityLabel.TextSize = 14
rarityLabel.Parent = tooltip

local function showTooltip(petName, x, y)
    local info = PetData.Pets[petName]
    if not info then return end
    tooltip.Visible = true
    tooltip.Position = UDim2.fromOffset(x + 15, y + 15)
    nameLabel.Text = info.Name
    strengthLabel.Text = "Strength: " .. tostring(info.Strength)
    rarityLabel.Text = "Rarity: " .. tostring(info.Rarity)
end

local function hideTooltip()
    tooltip.Visible = false
end

local function createIcon(petName, parent)
    local button = Instance.new("ImageButton")
    button.Size = UDim2.new(0, 60, 0, 60)
    button.BackgroundColor3 = Color3.fromRGB(80, 80, 80)
    button.Image = PetData.Pets[petName].Icon or ""
    button.Parent = parent
    return button
end

local function refreshInventory()
    for _, child in ipairs(equipFrame:GetChildren()) do
        if child:IsA("ImageButton") then
            child:Destroy()
        end
    end
    for _, child in ipairs(gridFrame:GetChildren()) do
        if child:IsA("ImageButton") then

            child:Destroy()
        end
    end

    local data = requestInventory:InvokeServer()


    for index = 1, 5 do
        local petName = data.EquippedPets[index]
        local btn = createIcon(petName or "", equipFrame)
        if petName then
            btn.MouseButton1Click:Connect(function()
                equipPetEvent:FireServer("unequip", index)
                refreshInventory()
            end)
            btn.MouseEnter:Connect(function()
                showTooltip(petName, UserInputService:GetMouseLocation().X, UserInputService:GetMouseLocation().Y)
            end)
            btn.MouseLeave:Connect(hideTooltip)
            btn.MouseMove:Connect(function(x, y)
                showTooltip(petName, x, y)
            end)
        else
            btn.Image = ""
        end
    end

    for petName, count in pairs(data.Inventory) do
        local btn = createIcon(petName, gridFrame)
        btn.MouseButton1Click:Connect(function()
            equipPetEvent:FireServer("equip", petName)
            refreshInventory()
        end)
        btn.MouseEnter:Connect(function()
            showTooltip(petName, UserInputService:GetMouseLocation().X, UserInputService:GetMouseLocation().Y)
        end)
        btn.MouseLeave:Connect(hideTooltip)
        btn.MouseMove:Connect(function(x, y)
            showTooltip(petName, x, y)
        end)
        if count > 1 then
            local countLabel = Instance.new("TextLabel")
            countLabel.Size = UDim2.new(0, 18, 0, 18)
            countLabel.Position = UDim2.new(1, -20, 1, -20)
            countLabel.BackgroundColor3 = Color3.fromRGB(0, 0, 0)
            countLabel.TextColor3 = Color3.new(1, 1, 1)
            countLabel.Text = tostring(count)
            countLabel.Font = Enum.Font.SourceSansBold
            countLabel.TextSize = 14
            countLabel.Parent = btn
        end

    end
end

refreshInventory()

